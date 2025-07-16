using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.InternshipPeriod;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/internship-periods")]
    [ApiController]
    public class InternshipPeriodsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InternshipPeriodsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/internship-periods
        [HttpGet]
        public async Task<ActionResult<object>> GetInternshipPeriods([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.InternshipPeriods
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
                .Select(p => new InternshipPeriodReadDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    AcademicYearId = p.AcademicYearId,
                    SemesterId = p.SemesterId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    DeletedAt = p.DeletedAt
                })
                .ToListAsync();
            return Ok(new { data = periods, total = totalCount, page, limit });
        }

        // GET: api/internship-periods/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<InternshipPeriodReadDto>> GetInternshipPeriod(int id)
        {
            var period = await _context.InternshipPeriods
                .Where(p => p.Id == id && p.DeletedAt == null)
                .Select(p => new InternshipPeriodReadDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    AcademicYearId = p.AcademicYearId,
                    SemesterId = p.SemesterId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    DeletedAt = p.DeletedAt
                })
                .FirstOrDefaultAsync();
            if (period == null) return NotFound();
            return Ok(period);
        }

        // POST: api/internship-periods
        [HttpPost]
        public async Task<ActionResult<InternshipPeriodReadDto>> PostInternshipPeriod(InternshipPeriodCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var period = new InternshipPeriod
            {
                Name = dto.Name,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                AcademicYearId = dto.AcademicYearId,
                SemesterId = dto.SemesterId,
                CreatedAt = DateTime.UtcNow
            };
            _context.InternshipPeriods.Add(period);
            await _context.SaveChangesAsync();
            var result = new InternshipPeriodReadDto
            {
                Id = period.Id,
                Name = period.Name,
                Description = period.Description,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                AcademicYearId = period.AcademicYearId,
                SemesterId = period.SemesterId,
                CreatedAt = period.CreatedAt,
                UpdatedAt = period.UpdatedAt,
                DeletedAt = period.DeletedAt
            };
            return CreatedAtAction(nameof(GetInternshipPeriod), new { id = period.Id }, result);
        }

        // PUT: api/internship-periods/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInternshipPeriod(int id, InternshipPeriodUpdateDto dto)
        {
            var period = await _context.InternshipPeriods.FindAsync(id);
            if (period == null || period.DeletedAt != null) return NotFound();
            if (dto.Name != null) period.Name = dto.Name;
            if (dto.Description != null) period.Description = dto.Description;
            if (dto.StartDate.HasValue) period.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) period.EndDate = dto.EndDate.Value;
            if (dto.AcademicYearId.HasValue) period.AcademicYearId = dto.AcademicYearId.Value;
            if (dto.SemesterId.HasValue) period.SemesterId = dto.SemesterId.Value;
            period.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new InternshipPeriodReadDto
            {
                Id = period.Id,
                Name = period.Name,
                Description = period.Description,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                AcademicYearId = period.AcademicYearId,
                SemesterId = period.SemesterId,
                CreatedAt = period.CreatedAt,
                UpdatedAt = period.UpdatedAt,
                DeletedAt = period.DeletedAt
            });
        }

        // SOFT DELETE: api/internship-periods/soft-delete/{id}
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteInternshipPeriod(int id)
        {
            var period = await _context.InternshipPeriods.FindAsync(id);
            if (period == null || period.DeletedAt != null) return NotFound();
            period.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PERMANENT DELETE: api/internship-periods/permanent-delete/{id}
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteInternshipPeriod(int id)
        {
            var period = await _context.InternshipPeriods.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
            if (period == null) return NotFound();
            _context.InternshipPeriods.Remove(period);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
} 