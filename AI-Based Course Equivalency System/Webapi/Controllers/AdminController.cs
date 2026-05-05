using Domine.Dtos;
using Domine.Interface;
using Infrastacture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _db;

        public AdminController(IAdminService adminService, ApplicationDbContext db)
        {
            _adminService = adminService;
            _db = db;
        }

        // ===================== DASHBOARD =====================

        /// <summary>
        /// GET api/admin/dashboard — إحصائيات عامة للأدمن
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = await _adminService.GetDashboardAsync();
            return Ok(dashboard);
        }

        // ===================== USER MANAGEMENT =====================

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] System.Text.Json.JsonElement body)
        {
            try
            {
                // استخراج القيم بشكل مرن
                var fullName = body.TryGetProperty("fullName", out var fn) ? fn.GetString() : null;
                var email    = body.TryGetProperty("email",    out var em) ? em.GetString() : null;
                var password = body.TryGetProperty("password", out var pw) ? pw.GetString() : null;

                // Role: يقبل رقم أو نص عربي أو إنجليزي
                Domine.Enum.UserRole role = Domine.Enum.UserRole.Student;
                if (body.TryGetProperty("role", out var roleEl))
                {
                    if (roleEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                        role = (Domine.Enum.UserRole)roleEl.GetInt32();
                    else
                    {
                        var roleStr = roleEl.GetString()?.ToLower().Trim() ?? "";
                        role = roleStr switch
                        {
                            "student" or "طالب" or "0" => Domine.Enum.UserRole.Student,
                            "doctor"  or "دكتور" or "1" => Domine.Enum.UserRole.Doctor,
                            "admin"   or "مدير" or "2"  => Domine.Enum.UserRole.Admin,
                            _ => Domine.Enum.UserRole.Student
                        };
                    }
                }

                if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return BadRequest(new { Message = "fullName, email, and password are required." });

                var dto = new Domine.Dtos.CreateUserDto
                {
                    FullName = fullName,
                    Email    = email,
                    Password = password,
                    Role     = role
                };

                var user = await _adminService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            try
            {
                var ok = await _adminService.UpdateUserAsync(id, dto);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var ok = await _adminService.DeleteUserAsync(id);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        // ===================== UNIVERSITY MANAGEMENT =====================

        [HttpGet("universities")]
        public async Task<IActionResult> GetUniversities()
        {
            var list = await _adminService.GetAllUniversitiesAsync();
            return Ok(list);
        }

        [HttpGet("universities/{id}")]
        public async Task<IActionResult> GetUniversity(int id)
        {
            var uni = await _adminService.GetUniversityByIdAsync(id);
            if (uni == null) return NotFound();
            return Ok(uni);
        }

        [HttpPost("universities")]
        public async Task<IActionResult> CreateUniversity([FromBody] CreateUniversityDto dto)
        {
            var uni = await _adminService.CreateUniversityAsync(dto);
            return CreatedAtAction(nameof(GetUniversity), new { id = uni.Id }, uni);
        }

        [HttpPut("universities/{id}")]
        public async Task<IActionResult> UpdateUniversity(int id, [FromBody] UpdateUniversityDto dto)
        {
            var ok = await _adminService.UpdateUniversityAsync(id, dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("universities/{id}")]
        public async Task<IActionResult> DeleteUniversity(int id)
        {
            try
            {
                var ok = await _adminService.DeleteUniversityAsync(id);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        // ===================== COURSE MANAGEMENT =====================

        [HttpGet("courses")]
        public async Task<IActionResult> GetCourses()
        {
            var list = await _adminService.GetAllCoursesAsync();
            return Ok(list);
        }

        [HttpGet("courses/{id}")]
        public async Task<IActionResult> GetCourse(int id)
        {
            var course = await _adminService.GetCourseByIdAsync(id);
            if (course == null) return NotFound();
            return Ok(course);
        }

        [HttpPost("courses")]
        public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
        {
            var course = await _adminService.CreateCourseAsync(dto);
            return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
        }

        [HttpPut("courses/{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto)
        {
            var ok = await _adminService.UpdateCourseAsync(id, dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("courses/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var ok = await _adminService.DeleteCourseAsync(id);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        // ===================== DEPARTMENTS (Faculties/Departments stub) =====================
        // Returns distinct department values from TransferRequests for display purposes

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _db.Set<Domine.Entity.TransferRequest>()
                .Where(t => t.DepartmentId != null)
                .Select(t => new { id = t.DepartmentId, name = $"قسم {t.DepartmentId}" })
                .Distinct()
                .ToListAsync();

            return Ok(departments);
        }
    }
}

// ── Shortcut routes so frontend calling /api/users and /api/departments works ──
namespace Webapi.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class UsersShortcutController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public UsersShortcutController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _adminService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _adminService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] Domine.Dtos.CreateUserDto dto)
        {
            try
            {
                var user = await _adminService.CreateUserAsync(dto);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] Domine.Dtos.UpdateUserDto dto)
        {
            try
            {
                var ok = await _adminService.UpdateUserAsync(id, dto);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var ok = await _adminService.DeleteUserAsync(id);
                if (!ok) return NotFound();
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }
    }

    [Route("api/departments")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class DepartmentsController : ControllerBase
    {
        private readonly Infrastacture.ApplicationDbContext _db;

        public DepartmentsController(Infrastacture.ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _db.Set<Domine.Entity.TransferRequest>()
                .Where(t => t.DepartmentId != null)
                .Select(t => t.DepartmentId)
                .Distinct()
                .Select(id => new { id, name = $"قسم {id}" })
                .ToListAsync();

            return Ok(departments);
        }
    }
}



