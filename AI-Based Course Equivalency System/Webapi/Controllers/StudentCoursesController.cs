using Domine.Dtos;
using Domine.Entity;
using Infrastacture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//using Microsoft.Exchange.WebServices.Data;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentCoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public StudentCoursesController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ... (existing code)

        [HttpPost]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create([FromForm] CreateStudentCourseDto dto, [FromForm] IFormFile? studentFile)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Get the current user's ID from claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!int.TryParse(userId, out var studentId))
                return BadRequest("Invalid user id.");

            // Create the StudentCourse entity from the DTO and userId
            var studentCourse = new StudentCourse
            {
                StudentId = studentId,
                CourseName = dto.CourseName,
                CreditHours = dto.CreditHours,
                Description = dto.Description,
                UniversityId = dto.UniversityId
            };

            // Optional: handle file upload if provided
            if (studentFile != null && studentFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads", "StudentCourses", studentId.ToString());
                Directory.CreateDirectory(uploadsDir);

                var uploadedFileName = Path.GetFileName(studentFile.FileName);
                var savePath = Path.Combine(uploadsDir, $"{Path.GetRandomFileName()}_{uploadedFileName}");

                await using (var fs = System.IO.File.Create(savePath))
                {
                    await studentFile.CopyToAsync(fs);
                }

                await using (var fs = System.IO.File.OpenRead(savePath))
                using (var sha = SHA256.Create())
                {
                    var hashBytes = await sha.ComputeHashAsync(fs);
                    var sb = new StringBuilder();
                    foreach (var b in hashBytes) sb.Append(b.ToString("x2"));

                    // persist metadata on the studentCourse entity before SaveChanges
                    studentCourse.UploadedFilePath = Path.GetRelativePath(_env.ContentRootPath, savePath).Replace("\\", "/");
                    studentCourse.UploadedFileName = uploadedFileName;
                    studentCourse.UploadedFileHash = sb.ToString();
                }
            }

            _db.StudentCourses.Add(studentCourse);
            await _db.SaveChangesAsync();

            // return created resource (so frontend can redirect/load)
            return CreatedAtAction(nameof(GetById), new { id = studentCourse.Id }, new { studentCourse.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var sc = await _db.StudentCourses
                .Include(s => s.University)
                .Where(s => s.Id == id)
                .Select(s => new StudentCourseDto
                {
                    Id = s.Id,
                    CourseName = s.CourseName,
                    CreditHours = s.CreditHours,
                    Description = s.Description,
                    UniversityId = s.UniversityId,
                    UniversityName = s.University != null ? s.University.Name : null
                })
                .FirstOrDefaultAsync();

            if (sc == null)
                return NotFound();

            return Ok(sc);
        }
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value
                    ?? User.FindFirst("role")?.Value;

            // Doctor: return all student courses
            if (role == "Doctor")
            {
                var allCourses = await _db.StudentCourses
                    .Include(s => s.University)
                    .Select(s => new StudentCourseDto
                    {
                        Id = s.Id,
                        CourseName = s.CourseName,
                        CreditHours = s.CreditHours,
                        Description = s.Description,
                        UniversityId = s.UniversityId,
                        UniversityName = s.University != null ? s.University.Name : null
                    })
                    .ToListAsync();

                return Ok(allCourses);
            }

            // Student: return only their own courses
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!int.TryParse(userId, out var studentId))
                return BadRequest("Invalid user id.");

            var courses = await _db.StudentCourses
                .Include(s => s.University)
                .Where(s => s.StudentId == studentId)
                .Select(s => new StudentCourseDto
                {
                    Id = s.Id,
                    CourseName = s.CourseName,
                    CreditHours = s.CreditHours,
                    Description = s.Description,
                    UniversityId = s.UniversityId,
                    UniversityName = s.University != null ? s.University.Name : null
                })
                .ToListAsync();

            return Ok(courses);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var studentId))
                return Unauthorized();

            var course = await _db.StudentCourses
                .FirstOrDefaultAsync(s => s.Id == id && s.StudentId == studentId);

            if (course == null)
                return NotFound(new { Message = "Course not found or not owned by you." });

            // Check if there are equivalency requests linked to this course
            var hasRequests = await _db.EquivalencyRequests
                .AnyAsync(r => r.StudentCourseId == id);

            if (hasRequests)
            {
                // Delete related equivalency requests first
                var relatedRequests = await _db.EquivalencyRequests
                    .Where(r => r.StudentCourseId == id)
                    .ToListAsync();

                _db.EquivalencyRequests.RemoveRange(relatedRequests);
            }

            // Delete the uploaded PDF file if it exists
            if (!string.IsNullOrWhiteSpace(course.UploadedFilePath))
            {
                var fullPath = Path.Combine(_env.ContentRootPath, course.UploadedFilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _db.StudentCourses.Remove(course);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
