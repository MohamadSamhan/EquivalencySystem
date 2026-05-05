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

        private static string DeriveRecommendation(int similarityPercent)
        {
            return similarityPercent switch
            {
                >= 85 => "AutoApproved",
                >= 60 => "RecommendToDoctor",
                _ => "ManualEvaluation"
            };
        }

        public async Task<EquivalencyRequestDto> CreateRequestAsync(int studentId, CreateEquivalencyRequestDto dto)
        {
            var studentCourse = await _db.StudentCourses.FindAsync(dto.StudentCourseId);
            if (studentCourse == null || studentCourse.StudentId != studentId)
                throw new InvalidOperationException("Student course not found or does not belong to student");

            var target = await _db.Courses.FindAsync(dto.TargetCourseId);
            if (target == null)
                throw new InvalidOperationException("Target course not found");

            // ── حاول استخراج النص من الـ PDF، وإذا ما في PDF استخدم اسم المادة ──
            string studentText = ExtractTextIfExists(studentCourse.UploadedFilePath)
                              ?? studentCourse.CourseName
                              ?? string.Empty;

            if (string.IsNullOrWhiteSpace(studentText))
                throw new InvalidOperationException("No text available for comparison.");

            string targetText = ExtractTextIfExists(target.ReferenceFilePath)
                             ?? target.Description
                             ?? target.CourseName
                             ?? string.Empty;

            if (string.IsNullOrWhiteSpace(targetText))
                throw new InvalidOperationException("No reference text available for target course.");

            var similarityNormalized = 0.0;
            try
            {
                similarityNormalized = _similarity.CalculateSimilarity(studentText, targetText);
                _logger?.LogInformation("Similarity result for StudentCourseId={Id} TargetCourseId={Tid}: {Sim}",
                    studentCourse.Id, target.Id, similarityNormalized);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Similarity calculation failed for StudentCourseId={Id} TargetCourseId={Tid}",
                    studentCourse.Id, target.Id);
                throw;
            }

            var similarityPercent = (int)Math.Round(similarityNormalized * 100.0);
            var recommendation = DeriveRecommendation(similarityPercent);

            // ── Auto-approve logic ─────────────────────────────────────────────────
            EquivalencyRequest? matchedApproved = null;

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
            }

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
            }

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
            }

            var isAutoApproved = matchedApproved != null;

            var request = new EquivalencyRequest
            {
                StudentId = studentId,
                StudentCourseId = studentCourse.Id,
                TargetCourseId = target.Id,
                SimilarityScore = isAutoApproved ? matchedApproved!.SimilarityScore : similarityPercent,
                Recommendation = isAutoApproved ? "AutoApproved" : recommendation,
                Status = isAutoApproved ? RequestStatus.Approved : RequestStatus.Pending,
                ReviewedByDoctorId = isAutoApproved ? matchedApproved!.ReviewedByDoctorId : null,
                ReviewerNotes = isAutoApproved ? $"Auto-approved based on previous decision (Request #{matchedApproved!.Id})" : null,
                ReviewedAt = isAutoApproved ? DateTime.UtcNow : null,
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
                .Include(r => r.ReviewedByDoctor)
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
            req.ReviewedByDoctorId = doctorId;
            req.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectRequestAsync(int requestId, int doctorId)
        {
            var req = await _db.EquivalencyRequests.FindAsync(requestId);
            if (req == null) return false;
            req.Status = RequestStatus.Rejected;
            req.ReviewedByDoctorId = doctorId;
            req.ReviewedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        private static EquivalencyRequestDto Map(EquivalencyRequest r)
        {
            var similarityPercent = r.SimilarityScore;
            var suggestion = r.Recommendation ?? DeriveRecommendation(similarityPercent);

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
                StudentFileName = fileName,
                ReviewerNotes = r.ReviewerNotes,
                ReviewedByDoctorName = r.ReviewedByDoctor?.FullName,
                ReviewedAt = r.ReviewedAt?.ToString("o")
            };
        }
    }
}
