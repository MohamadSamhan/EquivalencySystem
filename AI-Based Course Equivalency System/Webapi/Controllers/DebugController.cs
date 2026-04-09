using Domine.Entity;
using Domine.Interface;
using Infrastacture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ISimilarityService _sim;
        private readonly ILogger<DebugController> _logger;

        public DebugController(ApplicationDbContext db, IWebHostEnvironment env, ISimilarityService sim, ILogger<DebugController> logger)
        {
            _db = db;
            _env = env;
            _sim = sim;
            _logger = logger;
        }

        // GET /api/debug/preview/{studentCourseId}/{targetCourseId}
        [HttpGet("preview/{studentCourseId}/{targetCourseId}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Preview(int studentCourseId, int targetCourseId)
        {
            var sc = await _db.StudentCourses.FindAsync(studentCourseId);
            var tc = await _db.Courses.FindAsync(targetCourseId);
            if (sc == null || tc == null) return NotFound();

            string studentText = await ReadPdfTextIfExists(sc.UploadedFilePath);
            string targetText = await ReadPdfTextIfExists(tc.ReferenceFilePath);

            _logger.LogInformation("Preview: studentTextLen={Len1} targetTextLen={Len2}", studentText.Length, targetText.Length);

            var sim = _sim.CalculateSimilarity(studentText, targetText);

            return Ok(new
            {
                StudentCourseId = sc.Id,
                UploadedPath = sc.UploadedFilePath,
                StudentTextLength = studentText.Length,
                StudentSample = studentText.Length > 400 ? studentText.Substring(0, 400) : studentText,
                TargetCourseId = tc.Id,
                ReferencePath = tc.ReferenceFilePath,
                TargetTextLength = targetText.Length,
                TargetSample = targetText.Length > 400 ? targetText.Substring(0, 400) : targetText,
                Similarity = sim
            });
        }

        // GET /api/debug/test-openai
        [HttpGet("test-openai")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> TestOpenAi()
        {
            var testText = "This is a test to verify OpenAI API connectivity and embeddings";
            _logger.LogInformation("Testing OpenAI API with sample text: {Text}", testText);
            
            try
            {
                var embedding = await _sim.GetEmbeddingAsync(testText);
                if (embedding == null)
                {
                    return StatusCode(500, new
                    {
                        Status = "Failed",
                        Message = "OpenAI API returned null embedding. Check: 1) API key validity, 2) Account credits, 3) Network connectivity",
                        Suggestion = "Review application logs for detailed error messages"
                    });
                }

                return Ok(new
                {
                    Status = "Success",
                    Message = "OpenAI API is working correctly",
                    EmbeddingLength = embedding.Length,
                    TextLength = testText.Length,
                    FirstTenValues = embedding.Take(10).ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OpenAI API test failed");
                return StatusCode(500, new
                {
                    Status = "Error",
                    Message = "OpenAI API test failed",
                    Error = ex.Message,
                    Suggestion = "Check your OpenAI API key in appsettings.json or environment variables"
                });
            }
        }

        

        private async Task<string>ReadPdfTextIfExists(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;
            var trimmed = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("/", Path.DirectorySeparatorChar.ToString());
            var full = Path.Combine(_env.ContentRootPath, trimmed);
            if (!System.IO.File.Exists(full)) return string.Empty;

            var sb = new StringBuilder();
            using var doc = PdfDocument.Open(full);
            foreach (var page in doc.GetPages())
            {
                var text = page.Text;
                if (!string.IsNullOrEmpty(text)) sb.AppendLine(text);
            }
            return sb.ToString();
        }
    }
}
