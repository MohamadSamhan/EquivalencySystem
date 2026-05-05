using Domine.Dtos;
using Domine.Interface;
using Infrastacture;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Webapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UniversitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IAdminService _adminService;

        public UniversitiesController(ApplicationDbContext db, IAdminService adminService)
        {
            _db = db;
            _adminService = adminService;
        }

        // GET /api/universities  — متاح للكل (لاختيار الجامعة عند التسجيل)
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var list = await _db.Universities
                .OrderBy(u => u.Name)
                .Select(u => new { u.Id, u.Name, u.Country })
                .ToListAsync();

            return Ok(list);
        }

        // GET /api/universities/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var uni = await _adminService.GetUniversityByIdAsync(id);
            if (uni == null) return NotFound();
            return Ok(uni);
        }

        // POST /api/universities  — Admin فقط
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateUniversityDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var uni = await _adminService.CreateUniversityAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = uni.Id }, uni);
        }

        // PUT /api/universities/{id}  — Admin فقط
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUniversityDto dto)
        {
            var ok = await _adminService.UpdateUniversityAsync(id, dto);
            if (!ok) return NotFound();
            return NoContent();
        }

        // DELETE /api/universities/{id}  — Admin فقط
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
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
    }
}
