using Domine.Dtos;
using Domine.Entity;
using Domine.Enum;
using Domine.Interface;
using Infrastacture;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Service
{
    public class TranscriptService : ITranscriptService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISimilarityService _similarity;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<TranscriptService> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> FailGrades = new(StringComparer.OrdinalIgnoreCase)
        {
            "F", "FF", "FA", "W", "WF", "I", "0", "0.0"
        };

        public TranscriptService(
            ApplicationDbContext db,
            ISimilarityService similarity,
            IHttpClientFactory httpFactory,
            IConfiguration config,
            ILogger<TranscriptService> logger,
            IWebHostEnvironment env)
        {
            _db = db;
            _similarity = similarity;
            _httpFactory = httpFactory;
            _config = config;
            _logger = logger;
            _env = env;
        }

        // ── STEP 1: Extract courses only (no comparison, no save) ────────────
        public async Task<TranscriptExtractResultDto> ExtractOnlyAsync(
            int studentId,
            TransferType transferType,
            int? universityId,
            int? facultyId,
            int? departmentId,
            string? oldStudentId,
            string? universityName,
            string? majorName,
            Stream transcriptPdfStream,
            string originalFileName)
        {
            var (savePath, savedName) = await SaveTranscriptAsync(studentId, transcriptPdfStream, originalFileName);

            var transferRequest = new TransferRequest
            {
                StudentId = studentId,
                TransferType = transferType,
                UniversityId = universityId,
                FacultyId = facultyId,
                DepartmentId = departmentId,
                OldStudentId = oldStudentId ?? string.Empty,
                UniversityName = universityName ?? string.Empty,
                MajorName = majorName ?? string.Empty,
                TranscriptFilePath = Path.GetRelativePath(_env.ContentRootPath, savePath).Replace("\\", "/"),
                TranscriptFileName = savedName,
                CreatedAt = DateTime.UtcNow
            };

            _db.Set<TransferRequest>().Add(transferRequest);
            await _db.SaveChangesAsync();

            var rawText = ExtractPdfText(savePath);
            if (string.IsNullOrWhiteSpace(rawText))
                throw new InvalidOperationException("Transcript PDF contains no extractable text. Please upload a searchable PDF.");

            _logger.LogInformation("Extracted {Len} chars from transcript for transferRequestId={Id}", rawText.Length, transferRequest.Id);

            var courses = await ExtractCoursesWithGptAsync(rawText);
            _logger.LogInformation("GPT extracted {Count} courses for transferRequestId={Id}", courses.Count, transferRequest.Id);

            return new TranscriptExtractResultDto
            {
                TransferRequestId = transferRequest.Id,
                TransferType = transferType.ToString(),
                Courses = courses
            };
        }

        // ── STEP 2: Submit extracted courses for evaluation & save ───────────
        public async Task<TranscriptEvaluationResultDto> SubmitExtractedCoursesAsync(
            int studentId,
            SubmitExtractedCoursesDto dto)
        {
            var transferRequest = await _db.Set<TransferRequest>().FindAsync(dto.TransferRequestId)
                ?? throw new InvalidOperationException($"TransferRequest {dto.TransferRequestId} not found.");

            var internalCourses = await _db.Courses.ToListAsync();
            var resolvedUniversityId = transferRequest.UniversityId ?? 1;
            var results = new List<TranscriptCourseEvaluationDto>();

            foreach (var course in dto.Courses)
            {
                var evaluation = await EvaluateCourseAsync(studentId, resolvedUniversityId, course, internalCourses);
                results.Add(evaluation);
            }

            return new TranscriptEvaluationResultDto
            {
                TransferRequestId = transferRequest.Id,
                TransferType = transferRequest.TransferType.ToString(),
                TotalExtracted = results.Count,
                TotalPassed = results.Count(r => r.Passed),
                TotalSkipped = results.Count(r => r.Skipped),
                Results = results
            };
        }

        // ── Legacy: full pipeline in one step ────────────────────────────────
        public async Task<TranscriptEvaluationResultDto> ProcessInternalAsync(
            int studentId, Stream transcriptPdfStream, string originalFileName)
        {
            var extract = await ExtractOnlyAsync(studentId, TransferType.Internal,
                null, null, null, null, null, null, transcriptPdfStream, originalFileName);

            return await SubmitExtractedCoursesAsync(studentId,
                new SubmitExtractedCoursesDto { TransferRequestId = extract.TransferRequestId, Courses = extract.Courses });
        }

        public async Task<TranscriptEvaluationResultDto> ProcessExternalJordanianAsync(
            int studentId, int universityId, int facultyId, int departmentId,
            string oldStudentId, Stream transcriptPdfStream, string originalFileName)
        {
            var extract = await ExtractOnlyAsync(studentId, TransferType.ExternalJordanian,
                universityId, facultyId, departmentId, oldStudentId, null, null, transcriptPdfStream, originalFileName);

            return await SubmitExtractedCoursesAsync(studentId,
                new SubmitExtractedCoursesDto { TransferRequestId = extract.TransferRequestId, Courses = extract.Courses });
        }

        public async Task<TranscriptEvaluationResultDto> ProcessExternalNonJordanianAsync(
            int studentId, string universityName, string majorName,
            string oldStudentId, Stream transcriptPdfStream, string originalFileName)
        {
            var extract = await ExtractOnlyAsync(studentId, TransferType.ExternalNonJordanian,
                null, null, null, oldStudentId, universityName, majorName, transcriptPdfStream, originalFileName);

            return await SubmitExtractedCoursesAsync(studentId,
                new SubmitExtractedCoursesDto { TransferRequestId = extract.TransferRequestId, Courses = extract.Courses });
        }

        // ── Evaluate one course ───────────────────────────────────────────────
        private async Task<TranscriptCourseEvaluationDto> EvaluateCourseAsync(
            int studentId,
            int universityId,
            TranscriptCourseDto extracted,
            List<Course> internalCourses)
        {
            var eval = new TranscriptCourseEvaluationDto
            {
                CourseCode = extracted.CourseCode,
                CourseName = extracted.CourseName,
                Grade = extracted.Grade,
                CreditHours = extracted.CreditHours,
                Passed = IsGradePassing(extracted.Grade)
            };

            // ── شرط 1: الطالب ناجح (علامة >= 50) ─────────────────────────────
            if (!eval.Passed)
            {
                eval.Skipped = true;
                eval.Decision = "NotEquivalent";
                _logger.LogInformation("Course={Course} skipped — grade {Grade} is failing",
                    extracted.CourseName, extracted.Grade);
                return eval;
            }

            // ── شرط 2: مقارنة الأسماء بالـ AI ────────────────────────────────
            Course? bestMatch = null;
            double bestScore = 0.0;

            // Exact match أولاً
            var exactMatch = internalCourses.FirstOrDefault(c =>
                string.Equals(c.CourseName.Trim(), extracted.CourseName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                bestMatch = exactMatch;
                bestScore = 1.0;
            }
            else
            {
                foreach (var internalCourse in internalCourses)
                {
                    double score;
                    try
                    {
                        var internalText = await BuildCourseTextAsync(internalCourse);
                        score = _similarity.CalculateSimilarity(extracted.CourseName, internalText);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Similarity failed for {Course} vs {Internal}",
                            extracted.CourseName, internalCourse.CourseName);
                        score = 0.0;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMatch = internalCourse;
                    }
                }
            }

            eval.SimilarityScore = bestScore;
            eval.MatchedCourseId = bestMatch?.Id;
            eval.MatchedCourseName = bestMatch?.CourseName;

            var similarityPercent = (int)Math.Round(bestScore * 100.0);

            // ── شرط 3: نسبة التشابه >= 70% ────────────────────────────────────
            if (bestScore >= 0.70 && bestMatch != null)
            {
                eval.Decision = "Equivalent";
                eval.EquivalencyRequestId = await SaveEquivalencyRequestAsync(
                    studentId, universityId, extracted, bestMatch,
                    similarityPercent, RequestStatus.Approved, "Equivalent");
            }
            else
            {
                eval.Decision = "NotEquivalent";
                if (bestMatch != null)
                {
                    eval.EquivalencyRequestId = await SaveEquivalencyRequestAsync(
                        studentId, universityId, extracted, bestMatch,
                        similarityPercent, RequestStatus.Rejected, "NotEquivalent");
                }
            }

            _logger.LogInformation(
                "Course={Course} grade={Grade} match={Match} similarity={Score:P0} decision={Decision}",
                extracted.CourseName, extracted.Grade, bestMatch?.CourseName, bestScore, eval.Decision);

            return eval;
        }

        private static bool IsGradePassing(string grade)
        {
            if (string.IsNullOrWhiteSpace(grade)) return false;

            var trimmed = grade.Trim();

            if (double.TryParse(trimmed, out var numericGrade))
                return numericGrade >= 50.0;

            var failGrades = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "F", "FF", "FA", "W", "WF", "I", "U", "NC"
            };

            return !failGrades.Contains(trimmed);
        }

        private async Task<string> BuildCourseTextAsync(Course course)
        {
            if (!string.IsNullOrWhiteSpace(course.ReferenceFilePath))
            {
                var trimmed = course.ReferenceFilePath
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Replace("/", Path.DirectorySeparatorChar.ToString());
                var fullPath = Path.Combine(_env.ContentRootPath, trimmed);
                if (File.Exists(fullPath))
                {
                    var pdfText = ExtractPdfText(fullPath);
                    if (!string.IsNullOrWhiteSpace(pdfText))
                        return pdfText;
                }
            }

            return course.CourseName;
        }

        private async Task<int> SaveEquivalencyRequestAsync(
            int studentId, int universityId,
            TranscriptCourseDto extracted, Course internalCourse,
            int similarityPercent, RequestStatus status, string recommendation)
        {
            var studentCourse = new StudentCourse
            {
                StudentId = studentId,
                CourseName = extracted.CourseName,
                CreditHours = extracted.CreditHours,
                UniversityId = universityId
            };

            _db.StudentCourses.Add(studentCourse);
            await _db.SaveChangesAsync();

            var request = new EquivalencyRequest
            {
                StudentId = studentId,
                StudentCourseId = studentCourse.Id,
                TargetCourseId = internalCourse.Id,
                SimilarityScore = similarityPercent,
                Recommendation = recommendation,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            _db.EquivalencyRequests.Add(request);
            await _db.SaveChangesAsync();

            return request.Id;
        }

        private async Task<List<TranscriptCourseDto>> ExtractCoursesWithGptAsync(string rawText)
        {
            var apiKey = _config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey not configured");
            var baseUrl = _config["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/";

            var systemPrompt =
                "You are an expert academic transcript parser. " +
                "Extract ALL courses from the transcript text provided. " +
                "Return ONLY valid JSON matching this schema: " +
                "{\"courses\":[{\"courseCode\":\"CS101\",\"courseName\":\"...\",\"grade\":\"...\",\"creditHours\":3}]}. " +
                "courseCode is the course number/code (e.g. CS101, MATH201). If unknown use empty string. " +
                "creditHours must be an integer. If unknown use 3. " +
                "grade should be the raw grade string (A, B+, 85, etc). " +
                "Do not include any explanation, only JSON.";

            var requestBody = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = $"Transcript text:\n\n{rawText}" }
                },
                temperature = 0.1,
                max_tokens = 4096,
                response_format = new { type = "json_object" }
            };

            using var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromMinutes(2);

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync("chat/completions", content);
            var respText = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("GPT extraction failed: {Status} {Resp}", resp.StatusCode, respText);
                throw new InvalidOperationException($"OpenAI GPT call failed: {resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(respText);
            var messageContent = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            try
            {
                using var courseDoc = JsonDocument.Parse(messageContent);
                var courses = new List<TranscriptCourseDto>();

                if (courseDoc.RootElement.TryGetProperty("courses", out var arr))
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        courses.Add(new TranscriptCourseDto
                        {
                            CourseCode = item.TryGetProperty("courseCode", out var cc) ? cc.GetString() ?? string.Empty : string.Empty,
                            CourseName = item.TryGetProperty("courseName", out var cn) ? cn.GetString() ?? string.Empty : string.Empty,
                            Grade = item.TryGetProperty("grade", out var gr) ? gr.GetString() ?? string.Empty : string.Empty,
                            CreditHours = item.TryGetProperty("creditHours", out var ch) ? ch.GetInt32() : 3
                        });
                    }
                }

                return courses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse GPT course JSON: {Json}", messageContent);
                return new List<TranscriptCourseDto>();
            }
        }

        private async Task<(string fullPath, string savedName)> SaveTranscriptAsync(
            int studentId, Stream stream, string originalFileName)
        {
            var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads", "Transcripts", studentId.ToString());
            Directory.CreateDirectory(uploadsDir);

            var savedName = $"{Path.GetRandomFileName()}_{Path.GetFileName(originalFileName)}";
            var savePath = Path.Combine(uploadsDir, savedName);

            await using var fs = File.Create(savePath);
            await stream.CopyToAsync(fs);

            return (savePath, savedName);
        }

        private string ExtractPdfText(string fullPath)
        {
            try
            {
                var sb = new StringBuilder();
                using var doc = PdfDocument.Open(fullPath);
                foreach (var page in doc.GetPages())
                {
                    var text = page.Text;
                    if (!string.IsNullOrEmpty(text))
                        sb.AppendLine(text);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract PDF text from {Path}", fullPath);
                return string.Empty;
            }
        }
    }
}
