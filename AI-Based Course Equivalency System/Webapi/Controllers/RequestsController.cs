using Domine.Dtos;
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
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace Webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RequestsController : ControllerBase
    {
        private readonly IEquivalencyService _svc;
        private readonly ApplicationDbContext _db;
        private readonly ISimilarityService _similarity;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<RequestsController> _logger;

        public RequestsController(
            IEquivalencyService svc,
            ApplicationDbContext db,
            ISimilarityService similarity,
            IWebHostEnvironment env,
            ILogger<RequestsController> logger)
        {
            _svc = svc;
            _db = db;
            _similarity = similarity;
            _env = env;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create([FromBody] CreateEquivalencyRequestDto dto)
        {
            var studentId = GetCurrentUserId();
            if (studentId == null) return Unauthorized();

            var result = await _svc.CreateRequestAsync(studentId.Value, dto);
            // Return Created pointing to the collection GET (or use a specific GET by id if added)
            return CreatedAtAction(nameof(Get), null, result);
        }

        // New: accept a compare request from frontend, read student's uploaded PDF text,
        // compare with target (description or course PDF) using AI similarity, store percent.
        [HttpPost("compare")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Compare([FromBody] CreateEquivalencyRequestDto dto)
        {
            var studentId = GetCurrentUserId();
            if (studentId == null) return Unauthorized();

            var studentCourse = await _db.StudentCourses.FirstOrDefaultAsync(x => x.Id == dto.StudentCourseId && x.StudentId == studentId.Value);
            if (studentCourse == null) return NotFound(new { Message = "StudentCourse not found or not owned by you." });

            var targetCourse = await _db.Courses.FirstOrDefaultAsync(x => x.Id == dto.TargetCourseId);
            if (targetCourse == null) return NotFound(new { Message = "Target course not found." });

            // Read student uploaded PDF text (if any)
            string studentText = await ReadPdfTextIfExists(studentCourse.UploadedFilePath);
            if (string.IsNullOrWhiteSpace(studentText))
            {
                // fallback to description/name
                studentText = studentCourse.Description ?? studentCourse.CourseName ?? string.Empty;
            }

            // Read target text: prefer reference PDF, fallback to description/name
            string targetText = string.Empty;
            if (!string.IsNullOrWhiteSpace(targetCourse.ReferenceFilePath))
            {
                targetText = await ReadPdfTextIfExists(targetCourse.ReferenceFilePath);
            }
            if (string.IsNullOrWhiteSpace(targetText))
            {
                // fallback to description or course name
                targetText = targetCourse.Description ?? targetCourse.CourseName ?? string.Empty;
            }

            _logger.LogInformation("Compare: studentCourseId={SC} studentTextLen={L1} targetCourseId={TC} targetTextLen={L2}",
                studentCourse.Id, studentText.Length, targetCourse.Id, targetText.Length);

            // Call similarity AI
            double normalized = 0.0;
            try
            {
                normalized = _similarity.CalculateSimilarity(studentText, targetText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Similarity calculation failed");
                normalized = 0.0;
            }

            var percent = (int)Math.Round(normalized * 100.0);

            var request = new EquivalencyRequest
            {
                StudentId = studentId.Value,
                StudentCourseId = studentCourse.Id,
                TargetCourseId = targetCourse.Id,
                SimilarityScore = percent,
                Status = Domine.Enum.RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.EquivalencyRequests.Add(request);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = request.Id }, new
            {
                request.Id,
                request.SimilarityScore,
                request.Status,
                UploadedPath = studentCourse.UploadedFilePath,
                ReferencePath = targetCourse.ReferenceFilePath
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var id = GetCurrentUserId();

            if (role == "Doctor")
            {
                var list = await _svc.GetRequestsForDoctorAsync();
                return Ok(list);
            }

            if (role == "Student" && id != null)
            {
                var list = await _svc.GetRequestsForStudentAsync(id.Value);
                return Ok(list);
            }

            return Forbid();
        }

        // Add explicit route for frontend that calls /api/requests/my
        [HttpGet("my")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMy()
        {
            var id = GetCurrentUserId();
            if (id == null) return Unauthorized();

            var list = await _svc.GetRequestsForStudentAsync(id.Value);
            return Ok(list);
        }

        [HttpPut("approve")]
        [HttpPut("{requestId}/approve")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Approve([FromRoute] int requestId = 0, [FromQuery] int? queryRequestId = null)
        {
            var id = requestId > 0 ? requestId : queryRequestId ?? 0;
            if (id <= 0) return BadRequest("requestId is required");

            var doctorId = GetCurrentUserId();
            if (doctorId == null) return Unauthorized();

            var ok = await _svc.ApproveRequestAsync(id, doctorId.Value);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpPut("reject")]
        [HttpPut("{requestId}/reject")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Reject([FromRoute] int requestId = 0, [FromQuery] int? queryRequestId = null)
        {
            var id = requestId > 0 ? requestId : queryRequestId ?? 0;
            if (id <= 0) return BadRequest("requestId is required");

            var doctorId = GetCurrentUserId();
            if (doctorId == null) return Unauthorized();

            var ok = await _svc.RejectRequestAsync(id, doctorId.Value);
            if (!ok) return NotFound();
            return NoContent();
        }

        private int? GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var id)) return id;
            return null;
        }

        private async Task<string> ReadPdfTextIfExists(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;

            var trimmed = relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace("/", Path.DirectorySeparatorChar.ToString());
            var full = Path.Combine(_env.ContentRootPath, trimmed);
            if (!System.IO.File.Exists(full)) return string.Empty;

            // Offload PDF reading to a background thread to avoid CS1998
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                using var doc = PdfDocument.Open(full);
                foreach (var page in doc.GetPages())
                {
                    var text = page.Text;
                    if (!string.IsNullOrEmpty(text)) sb.AppendLine(text);
                }
                return sb.ToString();
            });
        }
    }
}
