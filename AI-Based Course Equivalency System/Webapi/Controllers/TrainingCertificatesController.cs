using Domine.Dtos;
using Domine.Entity;
using Domine.Enum;
using Infrastacture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Webapi.Controllers
{
    [Route("api/training-requests")]
    [ApiController]
    public class TrainingCertificatesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public TrainingCertificatesController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // POST /api/training-requests
        [HttpPost]
        [Authorize(Roles = "Student")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Create([FromForm] CreateTrainingCertificateRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var studentId = GetCurrentUserId();
            if (studentId == null) return Unauthorized();

            var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads", "TrainingCertificates", studentId.ToString());
            Directory.CreateDirectory(uploadsDir);

            var savedName = $"{Path.GetRandomFileName()}_{Path.GetFileName(dto.CertificateFile.FileName)}";
            var savePath = Path.Combine(uploadsDir, savedName);

            await using (var fs = System.IO.File.Create(savePath))
            {
                await dto.CertificateFile.CopyToAsync(fs);
            }

            var entity = new TrainingCertificateRequest
            {
                StudentId = studentId.Value,
                TrainingTitle = dto.TrainingTitle,
                TrainingProvider = dto.TrainingProvider,
                TrainingHours = dto.TrainingHours,
                CertificateFilePath = Path.GetRelativePath(_env.ContentRootPath, savePath).Replace("\\", "/"),
                CertificateFileName = dto.CertificateFile.FileName,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.TrainingCertificateRequests.Add(entity);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, Map(entity));
        }

        // GET /api/training-requests
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = GetCurrentUserId();

            IQueryable<TrainingCertificateRequest> query = _db.TrainingCertificateRequests
                .Include(t => t.Student)
                .Include(t => t.ReviewedByDoctor);

            if (role == "Student")
            {
                if (userId == null) return Unauthorized();
                query = query.Where(t => t.StudentId == userId.Value);
            }
            else if (role != "Doctor" && role != "Admin")
            {
                return Forbid();
            }

            var list = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return Ok(list.Select(Map));
        }

        // GET /api/training-requests/my
        [HttpGet("my")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMy()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var list = await _db.TrainingCertificateRequests
                .Include(t => t.Student)
                .Include(t => t.ReviewedByDoctor)
                .Where(t => t.StudentId == userId.Value)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(list.Select(Map));
        }

        // GET /api/training-requests/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = GetCurrentUserId();

            var entity = await _db.TrainingCertificateRequests
                .Include(t => t.Student)
                .Include(t => t.ReviewedByDoctor)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (entity == null) return NotFound();

            if (role == "Student" && entity.StudentId != userId)
                return Forbid();

            return Ok(Map(entity));
        }

        // GET /api/training-requests/{id}/certificate
        [HttpGet("{id:int}/certificate")]
        [Authorize(Roles = "Doctor,Admin,Student")]
        public async Task<IActionResult> DownloadCertificate(int id)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = GetCurrentUserId();

            var entity = await _db.TrainingCertificateRequests.FindAsync(id);
            if (entity == null) return NotFound();

            if (role == "Student" && entity.StudentId != userId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(entity.CertificateFilePath))
                return NotFound(new { Message = "No certificate file uploaded." });

            var fullPath = Path.Combine(
                _env.ContentRootPath,
                entity.CertificateFilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { Message = "Certificate file not found on server." });

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            var fileName = entity.CertificateFileName ?? "certificate.pdf";

            return File(fileBytes, "application/pdf", fileName);
        }

        // PUT /api/training-requests/{id}/approve
        [HttpPut("{id:int}/approve")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Approve(int id, [FromBody] ReviewDto? review = null)
        {
            var doctorId = GetCurrentUserId();
            if (doctorId == null) return Unauthorized();

            var entity = await _db.TrainingCertificateRequests.FindAsync(id);
            if (entity == null) return NotFound();

            entity.Status = RequestStatus.Approved;
            entity.ReviewedByDoctorId = doctorId.Value;
            entity.ReviewerNotes = review?.Notes;
            entity.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PUT /api/training-requests/{id}/reject
        [HttpPut("{id:int}/reject")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Reject(int id, [FromBody] ReviewDto? review = null)
        {
            var doctorId = GetCurrentUserId();
            if (doctorId == null) return Unauthorized();

            var entity = await _db.TrainingCertificateRequests.FindAsync(id);
            if (entity == null) return NotFound();

            entity.Status = RequestStatus.Rejected;
            entity.ReviewedByDoctorId = doctorId.Value;
            entity.ReviewerNotes = review?.Notes;
            entity.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static TrainingCertificateRequestDto Map(TrainingCertificateRequest t)
        {
            return new TrainingCertificateRequestDto
            {
                Id = t.Id,
                StudentId = t.StudentId,
                StudentName = t.Student?.FullName ?? string.Empty,
                TrainingTitle = t.TrainingTitle,
                TrainingProvider = t.TrainingProvider,
                TrainingHours = t.TrainingHours,
                CertificateFileUrl = t.CertificateFilePath != null
                    ? $"/api/training-requests/{t.Id}/certificate"
                    : null,
                CertificateFileName = t.CertificateFileName,
                Status = t.Status,
                ReviewerNotes = t.ReviewerNotes,
                ReviewedByDoctorName = t.ReviewedByDoctor?.FullName,
                ReviewedAt = t.ReviewedAt?.ToString("o"),
                CreatedAt = t.CreatedAt.ToString("o")
            };
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claim, out var id)) return id;
            return null;
        }
    }
}
