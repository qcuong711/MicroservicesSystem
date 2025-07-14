using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.ThesisPeriod;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/thesis-periods")]
    [ApiController]
    public class ThesisPeriodsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ThesisPeriodsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/thesis-periods
        [HttpGet]
        public async Task<ActionResult<object>> GetThesisPeriods([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.ThesisPeriods
                .Where(p => p.DeletedAt == null)
                .AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }
            var totalCount = await query.CountAsync();
            var periods = await query
                .OrderByDescending(p => p.StartDate)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new ThesisPeriodReadDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                })
                .ToListAsync();
            return Ok(new { data = periods, total = totalCount, page, limit });
        }

        // GET: api/thesis-periods/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ThesisPeriodReadDto>> GetThesisPeriod(int id)
        {
            var period = await _context.ThesisPeriods
                .Where(p => p.Id == id && p.DeletedAt == null)
                .Select(p => new ThesisPeriodReadDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate
                })
                .FirstOrDefaultAsync();
            if (period == null) return NotFound();
            return Ok(period);
        }

        // POST: api/thesis-periods
        [HttpPost]
        public async Task<ActionResult<ThesisPeriodReadDto>> PostThesisPeriod(ThesisPeriodCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var period = new ThesisPeriod
            {
                Name = dto.Name,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                CreatedAt = DateTime.UtcNow
            };
            _context.ThesisPeriods.Add(period);
            await _context.SaveChangesAsync();
            var result = new ThesisPeriodReadDto
            {
                Id = period.Id,
                Name = period.Name,
                StartDate = period.StartDate,
                EndDate = period.EndDate
            };
            return CreatedAtAction(nameof(GetThesisPeriod), new { id = period.Id }, result);
        }

        // PUT: api/thesis-periods/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutThesisPeriod(int id, ThesisPeriodUpdateDto dto)
        {
            var period = await _context.ThesisPeriods.FindAsync(id);
            if (period == null || period.DeletedAt != null) return NotFound();
            period.Name = dto.Name;
            period.StartDate = dto.StartDate;
            period.EndDate = dto.EndDate;
            period.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new ThesisPeriodReadDto
            {
                Id = period.Id,
                Name = period.Name,
                StartDate = period.StartDate,
                EndDate = period.EndDate
            });
        }

        // SOFT DELETE: api/thesis-periods/soft-delete/{id}
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteThesisPeriod(int id)
        {
            var period = await _context.ThesisPeriods.FindAsync(id);
            if (period == null || period.DeletedAt != null) return NotFound();
            period.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PERMANENT DELETE: api/thesis-periods/permanent-delete/{id}
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteThesisPeriod(int id)
        {
            var period = await _context.ThesisPeriods.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
            if (period == null) return NotFound();
            _context.ThesisPeriods.Remove(period);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
} 