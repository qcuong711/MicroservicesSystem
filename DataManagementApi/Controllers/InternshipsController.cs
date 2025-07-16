using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Internship;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InternshipsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public InternshipsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Internships
        [HttpGet]
        public async Task<ActionResult<object>> GetInternships(
            [FromQuery] int page = 1, 
            [FromQuery] int limit = 10, 
            [FromQuery] string search = "")
        {
            var query = _context.Internships
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Student)
                .Include(i => i.Partner)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.AcademicYear)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.Semester)
                .AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(i => (i.Student != null && i.Student.Name.Contains(search)) ||
                                         (i.Partner != null && i.Partner.Name.Contains(search)));
            }
            var totalCount = await query.CountAsync();
            var internshipDtos = await query
                .OrderByDescending(i => i.Id)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(i => new InternshipReadDto
                {
                    Id = i.Id,
                    Title = i.Title,
                    StudentId = i.StudentId,
                    StudentName = i.Student != null ? i.Student.Name : null,
                    PartnerId = i.PartnerId,
                    PartnerName = i.Partner != null ? i.Partner.Name : null,
                    InternshipPeriodId = i.InternshipPeriodId,
                    InternshipPeriodName = i.InternshipPeriod != null ? i.InternshipPeriod.Name : null,
                    ReportUrl = i.ReportUrl,
                    Grade = i.Grade,
                    DeletedAt = i.DeletedAt
                })
                .ToListAsync();
            return Ok(new 
            {
                data = internshipDtos,
                total = totalCount,
                page,
                limit
            });
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<InternshipReadDto>>> GetAllInternships()
        {
            var internships = await _context.Internships
                .Where(i => i.DeletedAt == null)
                .Include(i => i.Student)
                .Include(i => i.Partner)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.AcademicYear)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.Semester)
                .OrderByDescending(i => i.Id)
                .ToListAsync();
            var internshipDtos = internships.Select(i => new InternshipReadDto
            {
                Id = i.Id,
                Title = i.Title,
                StudentId = i.StudentId,
                StudentName = i.Student != null ? i.Student.Name : null,
                PartnerId = i.PartnerId,
                PartnerName = i.Partner != null ? i.Partner.Name : null,
                InternshipPeriodId = i.InternshipPeriodId,
                InternshipPeriodName = i.InternshipPeriod != null ? i.InternshipPeriod.Name : null,
                ReportUrl = i.ReportUrl,
                Grade = i.Grade,
                DeletedAt = i.DeletedAt
            }).ToList();
            return Ok(internshipDtos);
        }

        // GET: api/Internships/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InternshipReadDto>> GetInternship(int id)
        {
            var internship = await _context.Internships
                .Where(i => i.Id == id && i.DeletedAt == null)
                .Include(i => i.Student)
                .Include(i => i.Partner)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.AcademicYear)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.Semester)
                .Select(i => new InternshipReadDto
                {
                    Id = i.Id,
                    Title = i.Title,
                    StudentId = i.StudentId,
                    StudentName = i.Student != null ? i.Student.Name : null,
                    PartnerId = i.PartnerId,
                    PartnerName = i.Partner != null ? i.Partner.Name : null,
                    InternshipPeriodId = i.InternshipPeriodId,
                    InternshipPeriodName = i.InternshipPeriod != null ? i.InternshipPeriod.Name : null,
                    ReportUrl = i.ReportUrl,
                    Grade = i.Grade,
                    DeletedAt = i.DeletedAt
                })
                .FirstOrDefaultAsync();
            if (internship == null)
            {
                return NotFound();
            }
            return internship;
        }
        
        // POST: api/Internships
        [HttpPost]
        public async Task<ActionResult<InternshipReadDto>> PostInternship(InternshipCreateDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var student = await _context.Users.FindAsync(createDto.StudentId);
            if (student == null) return BadRequest(new { message = $"Sinh viên với ID {createDto.StudentId} không tồn tại" });
            var partner = await _context.Partners.FindAsync(createDto.PartnerId);
            if (partner == null) return BadRequest(new { message = $"Đối tác với ID {createDto.PartnerId} không tồn tại" });
            var internshipPeriod = await _context.InternshipPeriods.FindAsync(createDto.InternshipPeriodId);
            if (internshipPeriod == null) return BadRequest(new { message = $"Đợt thực tập với ID {createDto.InternshipPeriodId} không tồn tại" });
            var existingInternship = await _context.Internships
                .AnyAsync(i => i.StudentId == createDto.StudentId && 
                               i.InternshipPeriodId == createDto.InternshipPeriodId &&
                               i.DeletedAt == null);
            if (existingInternship)
            {
                return BadRequest(new { message = "Sinh viên đã có thực tập trong đợt này" });
            }
            var internshipEntity = new Internship
            {
                Title = createDto.Title,
                StudentId = createDto.StudentId,
                PartnerId = createDto.PartnerId,
                InternshipPeriodId = createDto.InternshipPeriodId,
                Grade = createDto.Grade,
                ReportUrl = createDto.ReportUrl,
                DeletedAt = null
            };
            _context.Internships.Add(internshipEntity);
            await _context.SaveChangesAsync();
            var createdInternship = await _context.Internships
                .Include(i => i.Student)
                .Include(i => i.Partner)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.AcademicYear)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.Semester)
                .Where(i => i.Id == internshipEntity.Id)
                .Select(i => new InternshipReadDto
                {
                    Id = i.Id,
                    Title = i.Title,
                    StudentId = i.StudentId,
                    StudentName = i.Student != null ? i.Student.Name : null,
                    PartnerId = i.PartnerId,
                    PartnerName = i.Partner != null ? i.Partner.Name : null,
                    InternshipPeriodId = i.InternshipPeriodId,
                    InternshipPeriodName = i.InternshipPeriod != null ? i.InternshipPeriod.Name : null,
                    ReportUrl = i.ReportUrl,
                    Grade = i.Grade,
                    DeletedAt = i.DeletedAt
                })
                .FirstOrDefaultAsync();
            return CreatedAtAction(nameof(GetInternship), new { id = internshipEntity.Id }, createdInternship);
        }

        // PUT: api/Internships/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutInternship(int id, InternshipUpdateDto updateDto)
        {
            var existingInternship = await _context.Internships.FindAsync(id);
            if (existingInternship == null || existingInternship.DeletedAt != null)
            {
                return NotFound("Kỳ thực tập không tồn tại hoặc đã bị xóa.");
            }
            if (updateDto.StudentId.HasValue) existingInternship.StudentId = updateDto.StudentId.Value;
            if (updateDto.PartnerId.HasValue) existingInternship.PartnerId = updateDto.PartnerId.Value;
            if (updateDto.InternshipPeriodId.HasValue) existingInternship.InternshipPeriodId = updateDto.InternshipPeriodId.Value;
            if (updateDto.Grade.HasValue) existingInternship.Grade = updateDto.Grade.Value;
            if (updateDto.ReportUrl != null) existingInternship.ReportUrl = updateDto.ReportUrl;
            if (updateDto.Title != null) existingInternship.Title = updateDto.Title;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InternshipExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            var updatedInternship = await _context.Internships
                .Include(i => i.Student)
                .Include(i => i.Partner)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.AcademicYear)
                .Include(i => i.InternshipPeriod)
                    .ThenInclude(p => p!.Semester)
                .Select(i => new InternshipReadDto
                {
                    Id = i.Id,
                    Title = i.Title,
                    StudentId = i.StudentId,
                    StudentName = i.Student != null ? i.Student.Name : null,
                    PartnerId = i.PartnerId,
                    PartnerName = i.Partner != null ? i.Partner.Name : null,
                    InternshipPeriodId = i.InternshipPeriodId,
                    InternshipPeriodName = i.InternshipPeriod != null ? i.InternshipPeriod.Name : null,
                    ReportUrl = i.ReportUrl,
                    Grade = i.Grade,
                    DeletedAt = i.DeletedAt
                })
                .FirstOrDefaultAsync();
            return Ok(updatedInternship);
        }
        
        // SOFT DELETE: api/internships/soft-delete/5
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteInternship(int id)
        {
            var internship = await _context.Internships.FindAsync(id);
            if (internship == null) return NotFound();
            if (internship.DeletedAt != null) return BadRequest("Kỳ thực tập đã được xóa.");

            internship.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        
        // GET: api/internships/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedInternships([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.Internships
                .Where(i => i.DeletedAt != null)
                 .Include(i => i.Student)
                .Include(i => i.Partner)
                .Include(i => i.InternshipPeriod)
                .AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                 query = query.Where(i => (i.Student != null && i.Student.Name.Contains(search)) ||
                                         (i.Partner != null && i.Partner.Name.Contains(search)));
            }
            var totalCount = await query.CountAsync();
            var internshipDtos = await query
                .OrderByDescending(i => i.DeletedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(i => new InternshipReadDto
                {
                    Id = i.Id,
                    Title = i.Title,
                    StudentId = i.StudentId,
                    StudentName = i.Student != null ? i.Student.Name : null,
                    PartnerId = i.PartnerId,
                    PartnerName = i.Partner != null ? i.Partner.Name : null,
                    InternshipPeriodId = i.InternshipPeriodId,
                    InternshipPeriodName = i.InternshipPeriod != null ? i.InternshipPeriod.Name : null,
                    ReportUrl = i.ReportUrl,
                    Grade = i.Grade,
                    DeletedAt = i.DeletedAt
                })
                .ToListAsync();
            return Ok(new { data = internshipDtos, total = totalCount, page, limit });
        }
        
        // BULK SOFT DELETE: api/internships/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");

            var internships = await _context.Internships.Where(i => ids.Contains(i.Id) && i.DeletedAt == null).ToListAsync();
            if (internships.Count == 0) return NotFound("Không tìm thấy kỳ thực tập hợp lệ để xóa.");

            foreach (var internship in internships)
            {
                internship.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa thành công {internships.Count} kỳ thực tập."});
        }
        
        // BULK RESTORE: api/internships/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            
            var internships = await _context.Internships.Where(i => ids.Contains(i.Id) && i.DeletedAt != null).ToListAsync();
            if (internships.Count == 0) return NotFound("Không tìm thấy kỳ thực tập hợp lệ để khôi phục.");
            
            foreach (var internship in internships)
            {
                internship.DeletedAt = null;
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã khôi phục thành công {internships.Count} kỳ thực tập."});
        }

        // BULK PERMANENT DELETE: api/internships/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");

            var internships = await _context.Internships.Where(r => ids.Contains(r.Id)).ToListAsync();
            if (internships.Count == 0) return NotFound("Không tìm thấy kỳ thực tập hợp lệ để xóa vĩnh viễn.");

            _context.Internships.RemoveRange(internships);
            
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa vĩnh viễn {internships.Count} kỳ thực tập." });
        }

        // Replaces the old DELETE endpoint
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteInternship(int id)
        {
            var internship = await _context.Internships.FindAsync(id);
            if (internship == null)
            {
                return NotFound();
            }

            _context.Internships.Remove(internship);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool InternshipExists(int id)
        {
            return _context.Internships.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}