using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.ThesisPeriod;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize]
        public async Task<ActionResult<object>> GetThesisPeriods([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.ThesisPeriods
                .Where(p => p.DeletedAt == null)
                .Include(p => p.AcademicYear)
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
                    StartDate = p.StartDate.ToString("o"),
                    EndDate = p.EndDate.ToString("o"),
                    AcademicYearId = p.AcademicYearId,
                    AcademicYearName = p.AcademicYear != null ? p.AcademicYear.Name : string.Empty
                })
                .ToListAsync();
            return Ok(new { data = periods, total = totalCount, page, limit });
        }

        // GET: api/thesis-periods/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedThesisPeriods([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.ThesisPeriods
                .Where(p => p.DeletedAt != null)
                .Include(p => p.AcademicYear)
                .AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }
            var totalCount = await query.CountAsync();
            var periods = await query
                .OrderByDescending(p => p.DeletedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new ThesisPeriodReadDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StartDate = p.StartDate.ToString("o"),
                    EndDate = p.EndDate.ToString("o"),
                    AcademicYearId = p.AcademicYearId,
                    AcademicYearName = p.AcademicYear != null ? p.AcademicYear.Name : string.Empty
                })
                .ToListAsync();
            return Ok(new { data = periods, total = totalCount, page, limit });
        }

        // GET: api/thesis-periods/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ThesisPeriodReadDto>> GetThesisPeriod(int id)
        {
            var period = await _context.ThesisPeriods
                .Where(p => p.Id == id && p.DeletedAt == null)
                .Include(p => p.AcademicYear)
                .Select(p => new ThesisPeriodReadDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    StartDate = p.StartDate.ToString("o"),
                    EndDate = p.EndDate.ToString("o"),
                    AcademicYearId = p.AcademicYearId,
                    AcademicYearName = p.AcademicYear != null ? p.AcademicYear.Name : string.Empty
                })
                .FirstOrDefaultAsync();
            if (period == null) return NotFound();
            return Ok(period);
        }

        // POST: api/thesis-periods
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ThesisPeriodReadDto>> PostThesisPeriod(ThesisPeriodCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var academicYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.Id == dto.AcademicYearId && y.DeletedAt == null);
            if (academicYear == null)
                return BadRequest("Năm học không hợp lệ hoặc đã bị xóa.");
            var period = new ThesisPeriod
            {
                Name = dto.Name,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                AcademicYearId = dto.AcademicYearId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ThesisPeriods.Add(period);
            await _context.SaveChangesAsync();
            var result = new ThesisPeriodReadDto
            {
                Id = period.Id,
                Name = period.Name,
                StartDate = period.StartDate.ToString("o"),
                EndDate = period.EndDate.ToString("o"),
                AcademicYearId = period.AcademicYearId,
                AcademicYearName = academicYear.Name
            };
            return CreatedAtAction(nameof(GetThesisPeriod), new { id = period.Id }, result);
        }

        // PUT: api/thesis-periods/{id}
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutThesisPeriod(int id, ThesisPeriodUpdateDto dto)
        {
            var period = await _context.ThesisPeriods.FindAsync(id);
            if (period == null || period.DeletedAt != null) return NotFound();
            var academicYear = await _context.AcademicYears.FirstOrDefaultAsync(y => y.Id == dto.AcademicYearId && y.DeletedAt == null);
            if (academicYear == null)
                return BadRequest("Năm học không hợp lệ hoặc đã bị xóa.");
            period.Name = dto.Name;
            period.StartDate = dto.StartDate;
            period.EndDate = dto.EndDate;
            period.AcademicYearId = dto.AcademicYearId;
            period.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new ThesisPeriodReadDto
            {
                Id = period.Id,
                Name = period.Name,
                StartDate = period.StartDate.ToString("o"),
                EndDate = period.EndDate.ToString("o"),
                AcademicYearId = period.AcademicYearId,
                AcademicYearName = academicYear.Name
            });
        }

        // SOFT DELETE: api/thesis-periods/soft-delete/{id}
        [HttpPost("soft-delete/{id}")]
        [Authorize]
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
        [Authorize]
        public async Task<IActionResult> PermanentDeleteThesisPeriod(int id)
        {
            var period = await _context.ThesisPeriods.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
            if (period == null) return NotFound();
            _context.ThesisPeriods.Remove(period);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/thesis-periods/bulk-restore
        [HttpPost("bulk-restore")]
        [Authorize]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("No ids provided.");
            var periods = await _context.ThesisPeriods
                .Where(p => ids.Contains(p.Id) && p.DeletedAt != null)
                .ToListAsync();
            foreach (var period in periods)
            {
                period.DeletedAt = null;
            }
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
} 