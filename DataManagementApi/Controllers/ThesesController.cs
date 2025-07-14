using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Thesis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThesesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ThesesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Theses
        [HttpGet]
        public async Task<ActionResult<object>> GetTheses(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string search = "",
            [FromQuery] DateTime? submissionDate = null
        )
        {
            try
            {
                var query = _context.Theses
                    .Where(t => t.DeletedAt == null)
                    .Include(t => t.Student)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(t =>
                        t.Title.Contains(search) ||
                        (t.Student != null && t.Student.FullName.Contains(search)) ||
                        (t.Supervisor != null && t.Supervisor.Name.Contains(search))
                    );
                }

                if (submissionDate.HasValue)
                {
                    var date = submissionDate.Value.Date;
                    query = query.Where(t => t.SubmissionDate.Date == date);
                }

                var total = await query.CountAsync();
                var theses = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(t => new ThesisReadDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        StudentId = t.StudentId,
                        StudentName = t.Student != null ? t.Student.FullName : null,
                        SupervisorId = t.SupervisorId,
                        SupervisorName = t.Supervisor != null ? t.Supervisor.Name : null,
                        ExaminerId = t.ExaminerId,
                        ExaminerName = t.Examiner != null ? t.Examiner.Name : null,
                        ThesisPeriodId = t.ThesisPeriodId,
                        ThesisPeriodName = t.ThesisPeriod != null ? t.ThesisPeriod.Name : null,
                        SubmissionDate = t.SubmissionDate,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        DeletedAt = t.DeletedAt
                    })
                    .ToListAsync();

                return Ok(new { data = theses, total });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // GET: api/Theses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ThesisReadDto>> GetThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses
                    .Where(t => t.Id == id && t.DeletedAt == null)
                    .Include(t => t.Student)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .FirstOrDefaultAsync();

                if (thesis == null)
                {
                    return NotFound();
                }

                var dto = new ThesisReadDto
                {
                    Id = thesis.Id,
                    Title = thesis.Title,
                    Description = thesis.Description,
                    StudentId = thesis.StudentId,
                    StudentName = thesis.Student != null ? thesis.Student.FullName : null,
                    SupervisorId = thesis.SupervisorId,
                    SupervisorName = thesis.Supervisor != null ? thesis.Supervisor.Name : null,
                    ExaminerId = thesis.ExaminerId,
                    ExaminerName = thesis.Examiner != null ? thesis.Examiner.Name : null,
                    ThesisPeriodId = thesis.ThesisPeriodId,
                    ThesisPeriodName = thesis.ThesisPeriod != null ? thesis.ThesisPeriod.Name : null,
                    SubmissionDate = thesis.SubmissionDate,
                    Status = thesis.Status,
                    CreatedAt = thesis.CreatedAt,
                    UpdatedAt = thesis.UpdatedAt,
                    DeletedAt = thesis.DeletedAt
                };

                return Ok(dto);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // PUT: api/Theses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutThesis(int id, ThesisUpdateDto thesisDto)
        {
            try
            {
                var existingThesis = await _context.Theses.FindAsync(id);
                if (existingThesis == null)
                {
                    return NotFound();
                }

                var student = await _context.Students.FindAsync(thesisDto.StudentId);
                if (student == null)
                {
                    return BadRequest("Sinh viên không tồn tại");
                }

                var supervisor = await _context.Lecturers.FindAsync(thesisDto.SupervisorId);
                if (supervisor == null)
                {
                    return BadRequest("Giảng viên hướng dẫn không tồn tại");
                }

                Lecturer? examiner = null;
                if (thesisDto.ExaminerId.HasValue)
                {
                    examiner = await _context.Lecturers.FindAsync(thesisDto.ExaminerId.Value);
                    if (examiner == null)
                    {
                        return BadRequest("Giảng viên phản biện không tồn tại");
                    }
                }

                var thesisPeriod = await _context.ThesisPeriods.FindAsync(thesisDto.ThesisPeriodId);
                if (thesisPeriod == null)
                {
                    return BadRequest("Đợt khóa luận không tồn tại");
                }

                existingThesis.Title = thesisDto.Title;
                existingThesis.Description = thesisDto.Description;
                existingThesis.StudentId = thesisDto.StudentId;
                existingThesis.SupervisorId = thesisDto.SupervisorId;
                existingThesis.ExaminerId = thesisDto.ExaminerId;
                existingThesis.ThesisPeriodId = thesisDto.ThesisPeriodId;
                existingThesis.SubmissionDate = thesisDto.SubmissionDate;
                existingThesis.Status = thesisDto.Status;
                existingThesis.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedThesis = await _context.Theses
                    .Where(t => t.Id == id)
                    .Include(t => t.Student)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .Select(t => new ThesisReadDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        StudentId = t.StudentId,
                        StudentName = t.Student != null ? t.Student.FullName : null,
                        SupervisorId = t.SupervisorId,
                        SupervisorName = t.Supervisor != null ? t.Supervisor.Name : null,
                        ExaminerId = t.ExaminerId,
                        ExaminerName = t.Examiner != null ? t.Examiner.Name : null,
                        ThesisPeriodId = t.ThesisPeriodId,
                        ThesisPeriodName = t.ThesisPeriod != null ? t.ThesisPeriod.Name : null,
                        SubmissionDate = t.SubmissionDate,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        DeletedAt = t.DeletedAt
                    })
                    .FirstOrDefaultAsync();

                if (updatedThesis == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi cập nhật dữ liệu");
                }

                return Ok(updatedThesis);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi cập nhật dữ liệu");
            }
        }

        // POST: api/Theses
        [HttpPost]
        public async Task<ActionResult<ThesisReadDto>> PostThesis(ThesisCreateDto thesisDto)
        {
            try
            {
                var student = await _context.Students.FindAsync(thesisDto.StudentId);
                if (student == null)
                {
                    return BadRequest("Sinh viên không tồn tại");
                }

                var supervisor = await _context.Lecturers.FindAsync(thesisDto.SupervisorId);
                if (supervisor == null)
                {
                    return BadRequest("Giảng viên hướng dẫn không tồn tại");
                }

                Lecturer? examiner = null;
                if (thesisDto.ExaminerId.HasValue)
                {
                    examiner = await _context.Lecturers.FindAsync(thesisDto.ExaminerId.Value);
                    if (examiner == null)
                    {
                        return BadRequest("Giảng viên phản biện không tồn tại");
                    }
                }

                var thesisPeriod = await _context.ThesisPeriods.FindAsync(thesisDto.ThesisPeriodId);
                if (thesisPeriod == null)
                {
                    return BadRequest("Đợt khóa luận không tồn tại");
                }

                var thesis = new Thesis
                {
                    Title = thesisDto.Title,
                    Description = thesisDto.Description,
                    StudentId = thesisDto.StudentId,
                    SupervisorId = thesisDto.SupervisorId,
                    ExaminerId = thesisDto.ExaminerId,
                    ThesisPeriodId = thesisDto.ThesisPeriodId,
                    SubmissionDate = thesisDto.SubmissionDate,
                    Status = thesisDto.Status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    DeletedAt = null
                };

                _context.Theses.Add(thesis);
                await _context.SaveChangesAsync();

                var createdThesis = await _context.Theses
                    .Where(t => t.Id == thesis.Id)
                    .Include(t => t.Student)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .Select(t => new ThesisReadDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        StudentId = t.StudentId,
                        StudentName = t.Student != null ? t.Student.FullName : null,
                        SupervisorId = t.SupervisorId,
                        SupervisorName = t.Supervisor != null ? t.Supervisor.Name : null,
                        ExaminerId = t.ExaminerId,
                        ExaminerName = t.Examiner != null ? t.Examiner.Name : null,
                        ThesisPeriodId = t.ThesisPeriodId,
                        ThesisPeriodName = t.ThesisPeriod != null ? t.ThesisPeriod.Name : null,
                        SubmissionDate = t.SubmissionDate,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        DeletedAt = t.DeletedAt
                    })
                    .FirstOrDefaultAsync();

                if (createdThesis == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi tạo mới khóa luận");
                }

                return CreatedAtAction(nameof(GetThesis), new { id = thesis.Id }, createdThesis);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi tạo mới khóa luận: {ex.Message}");
            }
        }

        // SOFT DELETE: api/Theses/soft-delete/5
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses.FindAsync(id);
                if (thesis == null)
                {
                    return NotFound();
                }
                thesis.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa mềm khóa luận");
            }
        }

        // PERMANENT DELETE: api/Theses/permanent-delete/5
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
                if (thesis == null)
                {
                    return NotFound();
                }
                _context.Theses.Remove(thesis);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa vĩnh viễn khóa luận");
            }
        }

        // RESTORE: api/Theses/restore/5
        [HttpPost("restore/{id}")]
        public async Task<IActionResult> RestoreThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
                if (thesis == null)
                {
                    return NotFound();
                }
                thesis.DeletedAt = null;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi khôi phục khóa luận");
            }
        }

        // GET DELETED: api/Theses/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedTheses([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Theses
                    .Where(t => t.DeletedAt != null)
                    .Include(t => t.Student)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                     query = query.Where(t =>
                        t.Title.Contains(search) ||
                        (t.Student != null && t.Student.FullName.Contains(search))
                    );
                }
                var total = await query.CountAsync();
                var theses = await query
                    .OrderByDescending(t => t.DeletedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(t => new ThesisReadDto {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        StudentId = t.StudentId,
                        StudentName = t.Student != null ? t.Student.FullName : null,
                        SupervisorId = t.SupervisorId,
                        SupervisorName = t.Supervisor != null ? t.Supervisor.Name : null,
                        ExaminerId = t.ExaminerId,
                        ExaminerName = t.Examiner != null ? t.Examiner.Name : null,
                        ThesisPeriodId = t.ThesisPeriodId,
                        ThesisPeriodName = t.ThesisPeriod != null ? t.ThesisPeriod.Name : null,
                        SubmissionDate = t.SubmissionDate,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        DeletedAt = t.DeletedAt
                    })
                    .ToListAsync();
                return Ok(new { data = theses, total });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi lấy danh sách khóa luận đã xóa");
            }
        }

        // BULK SOFT DELETE
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            try
            {
                var theses = await _context.Theses.Where(t => ids.Contains(t.Id)).ToListAsync();
                foreach (var thesis in theses)
                {
                    thesis.DeletedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return Ok(new { softDeleted = theses.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xóa mềm nhiều khóa luận: {ex.Message}");
            }
        }

        // BULK RESTORE
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            try
            {
                var theses = await _context.Theses.IgnoreQueryFilters().Where(t => ids.Contains(t.Id)).ToListAsync();
                foreach (var thesis in theses)
                {
                    thesis.DeletedAt = null;
                }
                await _context.SaveChangesAsync();
                return Ok(new { restored = theses.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi khôi phục nhiều khóa luận: {ex.Message}");
            }
        }
        
        // BULK PERMANENT DELETE
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            try
            {
                var theses = await _context.Theses.IgnoreQueryFilters().Where(t => ids.Contains(t.Id)).ToListAsync();
                _context.Theses.RemoveRange(theses);
                await _context.SaveChangesAsync();
                return Ok(new { permanentlyDeleted = theses.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xóa vĩnh viễn nhiều khóa luận: {ex.Message}");
            }
        }

        // DELETE: api/Theses/5 (This is now replaced by soft-delete and permanent-delete)
        // I'll comment it out to avoid confusion, but it could be removed entirely.
        /*
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses.FindAsync(id);
                if (thesis == null)
                {
                    return NotFound();
                }

                _context.Theses.Remove(thesis);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa khóa luận");
            }
        }
        */

        private bool ThesisExists(int id)
        {
            return _context.Theses.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}