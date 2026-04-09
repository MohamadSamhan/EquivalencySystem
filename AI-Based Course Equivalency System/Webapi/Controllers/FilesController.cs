using Domine.Entity;
using Domine.Interface;
using Infrastacture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<FilesController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly ISimilarityService _similarity;

        public FilesController(IWebHostEnvironment env, ILogger<FilesController> logger, ApplicationDbContext db, ISimilarityService similarity)
        {
            _env = env;
            _logger = logger;
            _db = db;
            _similarity = similarity;
        }

        // POST /api/files/compare
        // form fields: studentCourseId (int), targetCourseId (int), studentFile (file)
        [HttpPost("compare")]
        [Authorize(Roles = "Student")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Compare([FromForm] int studentCourseId, [FromForm] int targetCourseId, [FromForm] IFormFile studentFile)
        {
            if (studentFile == null || studentFile.Length == 0)
                return BadRequest(new { Message = "No file uploaded." });

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var studentCourse = await _db.StudentCourses.FirstOrDefaultAsync(s => s.Id == studentCourseId && s.StudentId == currentUserId.Value);
            if (studentCourse == null)
                return NotFound(new { Message = "StudentCourse not found or not owned by current user." });

            var targetCourse = await _db.Courses.FindAsync(targetCourseId);
            if (targetCourse == null)
                return NotFound(new { Message = "Target course not found." });

            // Save uploaded file
            var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads", "StudentCourses", currentUserId.Value.ToString());
            Directory.CreateDirectory(uploadsDir);

            var savedFileName = $"{Path.GetRandomFileName()}_{Path.GetFileName(studentFile.FileName)}";
            var savePath = Path.Combine(uploadsDir, savedFileName);

            try
            {
                await using (var fs = System.IO.File.Create(savePath))
                {
                    await studentFile.CopyToAsync(fs);
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(savePath);
                var fileHash = ComputeSha256(fileBytes);

                // Persist uploaded file metadata to studentCourse
                studentCourse.UploadedFilePath = Path.GetRelativePath(_env.ContentRootPath, savePath).Replace("\\", "/");
                studentCourse.UploadedFileName = studentFile.FileName;
                studentCourse.UploadedFileHash = fileHash;

                await _db.SaveChangesAsync();

                // Determine reference (course) file path
                string referencePath;
                if (!string.IsNullOrWhiteSpace(targetCourse.ReferenceFilePath) && Path.GetExtension(targetCourse.ReferenceFilePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    referencePath = Path.Combine(_env.ContentRootPath, targetCourse.ReferenceFilePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                }
                else
                {
                    referencePath = Path.Combine(_env.ContentRootPath, "Files", "CourseFiles", $"{targetCourseId}.pdf");
                }

                if (!System.IO.File.Exists(referencePath))
                {
                    _logger.LogWarning("Reference file not found at {Path}", referencePath);
                    return NotFound(new { Message = "Reference file not found on server.", ReferencePath = referencePath });
                }

                // Extract text from both PDFs (requires UglyToad.PdfPig)
                string studentText = ExtractTextFromPdf(savePath);
                string referenceText = ExtractTextFromPdf(referencePath);

                var similarityNormalized = _similarity.CalculateSimilarity(studentText, referenceText);
                var similarityPercent = (int)Math.Round(similarityNormalized * 100.0);

                return Ok(new
                {
                    Similarity = similarityNormalized,
                    SimilarityPercent = similarityPercent,
                    Uploaded = studentCourse.UploadedFilePath,
                    UploadedFileName = studentCourse.UploadedFileName,
                    UploadedFileHash = studentCourse.UploadedFileHash,
                    ReferenceFile = Path.GetRelativePath(_env.ContentRootPath, referencePath).Replace("\\", "/")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling uploaded file");
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Error processing files." });
            }
        }

        // GET /api/files/download/{studentCourseId}
        // Allows Doctor to download student's uploaded PDF
        [HttpGet("download/{studentCourseId}")]
        [Authorize(Roles = "Doctor,Student")]
        public async Task<IActionResult> Download(int studentCourseId)
        {
            var course = await _db.StudentCourses.FindAsync(studentCourseId);
            if (course == null)
                return NotFound(new { Message = "Student course not found." });

            // If Student, only allow downloading their own file
            var role = User.FindFirst(ClaimTypes.Role)?.Value
                    ?? User.FindFirst("role")?.Value;
            if (role == "Student")
            {
                var userId = GetCurrentUserId();
                if (userId == null || course.StudentId != userId.Value)
                    return Forbid();
            }

            if (string.IsNullOrWhiteSpace(course.UploadedFilePath))
                return NotFound(new { Message = "No file uploaded for this course." });

            var fullPath = Path.Combine(
                _env.ContentRootPath,
                course.UploadedFilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { Message = "File not found on server." });

            var fileName = course.UploadedFileName ?? "document.pdf";
            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);

            return File(fileBytes, "application/pdf", fileName);
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static string ExtractTextFromPdf(string pdfPath)
        {
            using var doc = PdfDocument.Open(pdfPath);
            var sb = new StringBuilder();
            foreach (var page in doc.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    sb.AppendLine(text);
                }
            }
            return sb.ToString();
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id)) return id;
            return null;
        }
    }
}
