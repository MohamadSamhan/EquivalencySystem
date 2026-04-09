using Domine.Dtos;
using Domine.Entity;
using Infrastacture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Webapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CoursesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IEnumerable<CourseDto>> Get()
        {
            return await _db.Courses
                .Select(c => new CourseDto
                {
                    Id = c.Id,
                    CourseName = c.CourseName,
                    CreditHours = c.CreditHours,
                    Description = c.Description
                })
                .ToListAsync();
        }

        // Protected: Doctor only (JWT to be applied later)
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Create([FromBody] CreateCourseDto dto)
        {
            var course = new Course
            {
                CourseName = dto.CourseName,
                CreditHours = dto.CreditHours,
                Description = dto.Description
            };

            _db.Courses.Add(course);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = course.Id });
        }
    }
}
