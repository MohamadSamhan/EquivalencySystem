using Domine.Dtos;
using Domine.Entity;
using Domine.Enum;
using Domine.Interface;
using Infrastacture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using Tesseract;
using System.Drawing;
using UglyToad.PdfPig;

namespace Service
{
    public class EquivalencyService : IEquivalencyService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISimilarityService _similarity;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<EquivalencyService>? _logger;

        public EquivalencyService(ApplicationDbContext db, ISimilarityService similarity, IWebHostEnvironment env, ILogger<EquivalencyService>? logger = null)
        {
            _db = db;
            _similarity = similarity;
            _env = env;
            _logger = logger;
        }

        private string? ExtractTextIfExists(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                // Normalize incoming path (handle absolute and relative)
                var normalized = path.Replace("/", Path.DirectorySeparatorChar.ToString()).Trim();
                string full = Path.IsPathRooted(normalized)
                    ? Path.GetFullPath(normalized)
                    : Path.Combine(_env.ContentRootPath, normalized);

                _logger?.LogDebug("ExtractTextIfExists resolving path. Input='{Path}' Full='{Full}'", path, full);

                if (!File.Exists(full))
                {
                    _logger?.LogDebug("PDF file not found: {Full}", full);
                    return null;
                }

                var sb = new StringBuilder();
                using var doc = PdfDocument.Open(full);
                foreach (var page in doc.GetPages())
                {
                    var text = page.Text;
                    if (!string.IsNullOrWhiteSpace(text))
                        sb.AppendLine(text);
                }

                var result = sb.ToString();
                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger?.LogDebug("PDF contained no extractable text: {Full}", full);
                    return null;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to extract PDF text from {Path}", path);
                return null;
            }
        }

        public async Task<EquivalencyRequestDto> CreateRequestAsync(int studentId, CreateEquivalencyRequestDto dto)
        {
            var studentCourse = await _db.StudentCourses.FindAsync(dto.StudentCourseId);
            if (studentCourse == null || studentCourse.StudentId != studentId)
                throw new InvalidOperationException("Student course not found or does not belong to student");

            var target = await _db.Courses.FindAsync(dto.TargetCourseId);
            if (target == null)
                throw new InvalidOperationException("Target course not found");

            // Read text from student uploaded PDF (required)
            string? studentText = ExtractTextIfExists(studentCourse.UploadedFilePath);
            if (string.IsNullOrWhiteSpace(studentText))
            {
                _logger?.LogWarning("Student PDF text empty or missing for StudentCourseId={Id} UploadedFilePath={Path}", studentCourse.Id, studentCourse.UploadedFilePath);
                throw new InvalidOperationException("Uploaded student PDF not found or contains no extractable text. Please upload a searchable PDF.");
            }

            // Read text from course reference PDF (required)
            string? targetText = ExtractTextIfExists(target.ReferenceFilePath);
            if (string.IsNullOrWhiteSpace(targetText))
            {
                // try fallback location Files/CourseFiles/{id}.pdf
                var fallback = Path.Combine(_env.ContentRootPath, "Files", "CourseFiles", $"{target.Id}.pdf");
                targetText = ExtractTextIfExists(Path.GetRelativePath(_env.ContentRootPath, fallback));
            }
            if (string.IsNullOrWhiteSpace(targetText))
            {
                _logger?.LogWarning("Reference PDF text empty or missing for TargetCourseId={Id} ReferenceFilePath={Path}", target.Id, target.ReferenceFilePath);
                throw new InvalidOperationException("Reference course PDF not found or contains no extractable text. Please ensure a searchable reference PDF exists.");
            }

            // Diagnostic logs
            _logger?.LogDebug("Student text length={L1} preview='{P1}'", studentText.Length, studentText.Length > 200 ? studentText.Substring(0, 200) : studentText);
            _logger?.LogDebug("Target text length={L2} preview='{P2}'", targetText.Length, targetText.Length > 200 ? targetText.Substring(0, 200) : targetText);

            // Compute similarity via AI service (normalized 0.0 - 1.0)
            var similarityNormalized = 0.0;
            try
            {
                similarityNormalized = _similarity.CalculateSimilarity(studentText, targetText);
                _logger?.LogInformation("Similarity result for StudentCourseId={Id} TargetCourseId={Tid}: {Sim}", studentCourse.Id, target.Id, similarityNormalized);
            }
            catch (Exception ex)
            {
                // surface the error so you can fix the similarity provider rather than silently using 0
                _logger?.LogError(ex, "Similarity calculation failed for StudentCourseId={Id} TargetCourseId={Tid}", studentCourse.Id, target.Id);
                throw;
            }

            var similarityPercent = (int)Math.Round(similarityNormalized * 100.0);

            // --- Auto-approve logic ---
            // Check if there's a previously approved request that matches this one.
            // Match by: same university + same target course + (same file hash OR same course name)
            EquivalencyRequest? matchedApproved = null;

            // 1) Try matching by file hash (same exact PDF uploaded before)
            if (!string.IsNullOrWhiteSpace(studentCourse.UploadedFileHash))
            {
                matchedApproved = await _db.EquivalencyRequests
                    .Include(r => r.StudentCourse)
                    .Where(r => r.Status == RequestStatus.Approved
                             && r.TargetCourseId == target.Id
                             && r.SimilarityScore > 0
                             && r.StudentCourse != null
                             && r.StudentCourse.UploadedFileHash == studentCourse.UploadedFileHash
                             && r.StudentCourse.UniversityId == studentCourse.UniversityId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                if (matchedApproved != null)
                    _logger?.LogInformation("Auto-approve match by FILE HASH for StudentCourseId={Id}, matched RequestId={Rid}",
                        studentCourse.Id, matchedApproved.Id);
            }

            // 2) If no hash match, try matching by course name from same university
            if (matchedApproved == null)
            {
                var normalizedName = (studentCourse.CourseName ?? string.Empty).ToLower().Trim();
                matchedApproved = await _db.EquivalencyRequests
                    .Include(r => r.StudentCourse)
                    .Where(r => r.Status == RequestStatus.Approved
                             && r.TargetCourseId == target.Id
                             && r.SimilarityScore > 0
                             && r.StudentCourse != null
                             && r.StudentCourse.UniversityId == studentCourse.UniversityId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync(r =>
                        r.StudentCourse!.CourseName.ToLower().Trim() == normalizedName);

                if (matchedApproved != null)
                    _logger?.LogInformation("Auto-approve match by COURSE NAME for StudentCourseId={Id}, matched RequestId={Rid}",
                        studentCourse.Id, matchedApproved.Id);
            }

            // 3) If no match by name, try matching by high similarity score on same content
            if (matchedApproved == null && similarityPercent >= 85)
            {
                matchedApproved = await _db.EquivalencyRequests
                    .Include(r => r.StudentCourse)
                    .Where(r => r.Status == RequestStatus.Approved
                             && r.TargetCourseId == target.Id
                             && r.SimilarityScore >= 85
                             && r.StudentCourse != null
                             && r.StudentCourse.UniversityId == studentCourse.UniversityId)
                    .OrderByDescending(r => r.CreatedAt)
                    .FirstOrDefaultAsync();

                if (matchedApproved != null)
                    _logger?.LogInformation("Auto-approve match by HIGH SIMILARITY (>= 85%) for StudentCourseId={Id}, matched RequestId={Rid}",
                        studentCourse.Id, matchedApproved.Id);
            }

            var request = new EquivalencyRequest
            {
                StudentId = studentId,
                StudentCourseId = studentCourse.Id,
                TargetCourseId = target.Id,
                SimilarityScore = matchedApproved != null ? matchedApproved.SimilarityScore : similarityPercent,
                Status = matchedApproved != null ? RequestStatus.Approved : RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.EquivalencyRequests.Add(request);
            await _db.SaveChangesAsync();

            return Map(request);
        }

        public async Task<IEnumerable<EquivalencyRequestDto>> GetRequestsForDoctorAsync()
        {
            var list = await _db.EquivalencyRequests
                .Include(r => r.StudentCourse)
                .Include(r => r.TargetCourse)
                .Include(r => r.Student)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return list.Select(Map);
        }

        public async Task<IEnumerable<EquivalencyRequestDto>> GetRequestsForStudentAsync(int studentId)
        {
            var list = await _db.EquivalencyRequests
                .Include(r => r.StudentCourse)
                .Include(r => r.TargetCourse)
                .Include(r => r.Student)
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return list.Select(Map);
        }

        public async Task<bool> ApproveRequestAsync(int requestId, int doctorId)
        {
            var req = await _db.EquivalencyRequests.FindAsync(requestId);
            if (req == null) return false;
            req.Status = RequestStatus.Approved;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectRequestAsync(int requestId, int doctorId)
        {
            var req = await _db.EquivalencyRequests.FindAsync(requestId);
            if (req == null) return false;
            req.Status = RequestStatus.Rejected;
            await _db.SaveChangesAsync();
            return true;
        }

        private static EquivalencyRequestDto Map(EquivalencyRequest r)
        {
            var similarityPercent = r.SimilarityScore;

            // Derive suggestion based on similarity score
            var suggestion = similarityPercent switch
            {
                >= 85 => "AutoApproved",
                >= 60 => "RecommendToDoctor",
                _ => "ManualEvaluation"
            };

            // Build file download URL if file exists
            string? fileUrl = null;
            string? fileName = null;
            if (r.StudentCourse != null && !string.IsNullOrWhiteSpace(r.StudentCourse.UploadedFilePath))
            {
                fileUrl = $"/api/files/download/{r.StudentCourseId}";
                fileName = r.StudentCourse.UploadedFileName;
            }

            return new EquivalencyRequestDto
            {
                Id = r.Id,
                StudentCourseId = r.StudentCourseId,
                TargetCourseId = r.TargetCourseId,
                SimilarityScore = similarityPercent,
                Similarity = similarityPercent,
                Status = r.Status,
                Suggestion = suggestion,
                CreatedAt = r.CreatedAt.ToString("o"),
                StudentName = r.Student?.FullName ?? string.Empty,
                StudentCourseName = r.StudentCourse?.CourseName ?? string.Empty,
                TargetCourseName = r.TargetCourse?.CourseName ?? string.Empty,
                StudentFileUrl = fileUrl,
                StudentFileName = fileName
            };
        }
    }
}
