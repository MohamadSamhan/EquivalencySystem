using Infrastacture;
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

        public UniversitiesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var list = await _db.Universities
                .OrderBy(u => u.Name)
                .Select(u => new { u.Id, u.Name, u.Country })
                .ToListAsync();

            return Ok(list);
        }
    }
}
