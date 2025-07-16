using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Semester;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/semesters")]
    [ApiController]
    public class SemestersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SemestersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Semesters
        [HttpGet]
        public async Task<ActionResult<object>> GetSemesters([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Semesters
                    .Where(s => s.DeletedAt == null)
                    .Include(s => s.AcademicYear)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => s.Name.Contains(search) || (s.AcademicYear != null && s.AcademicYear.Name.Contains(search)));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var semesters = await query
                    .OrderByDescending(s => s.AcademicYear.StartDate)
                    .ThenBy(s => s.Name)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(s => new SemesterReadDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        AcademicYearId = s.AcademicYearId,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        DeletedAt = s.DeletedAt
                    })
                    .ToListAsync();

                // Return paginated response format
                return Ok(new
                {
                    data = semesters,
                    total = totalCount,
                    page = page,
                    limit = limit
                });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }
        
        // GET: api/Semesters/all
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<SemesterReadDto>>> GetAllSemesters()
        {
            try
            {
                var semesters = await _context.Semesters
                    .Where(d => d.DeletedAt == null)
                    .OrderByDescending(s => s.AcademicYear.StartDate)
                    .ThenBy(s => s.Name)
                    .Select(s => new SemesterReadDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        AcademicYearId = s.AcademicYearId,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        DeletedAt = s.DeletedAt
                    })
                    .ToListAsync();
                return Ok(semesters);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // GET: api/Semesters/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SemesterReadDto>> GetSemester(int id)
        {
            try
            {
                var semester = await _context.Semesters
                    .Where(s => s.DeletedAt == null)
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (semester == null)
                {
                    return NotFound();
                }

                var dto = new SemesterReadDto
                {
                    Id = semester.Id,
                    Name = semester.Name,
                    AcademicYearId = semester.AcademicYearId,
                    StartDate = semester.StartDate,
                    EndDate = semester.EndDate,
                    DeletedAt = semester.DeletedAt
                };

                return Ok(dto);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // PUT: api/Semesters/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSemester(int id, SemesterUpdateDto semesterDto)
        {
            var semester = await _context.Semesters.FindAsync(id);
            if (semester == null || semester.DeletedAt != null)
            {
                return NotFound();
            }

            // Validate academic year exists
            var academicYearExists = await _context.AcademicYears.AnyAsync(ay => ay.Id == semesterDto.AcademicYearId && ay.DeletedAt == null);
            if (!academicYearExists)
            {
                return BadRequest(new { message = "Năm học không tồn tại" });
            }
            
            // Check for duplicate semester name in the same academic year
            var existingSemester = await _context.Semesters
                .AnyAsync(s => s.Name == semesterDto.Name && s.AcademicYearId == semesterDto.AcademicYearId && s.Id != id && s.DeletedAt == null);
            if (existingSemester)
            {
                return BadRequest(new { message = "Học kỳ đã tồn tại trong năm học này" });
            }

            semester.Name = semesterDto.Name;
            semester.AcademicYearId = semesterDto.AcademicYearId;
            semester.StartDate = semesterDto.StartDate;
            semester.EndDate = semesterDto.EndDate;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SemesterExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi cập nhật dữ liệu: {ex.Message}");
            }

            var updatedSemester = await _context.Semesters.Include(s => s.AcademicYear).FirstOrDefaultAsync(s => s.Id == id);
            if (updatedSemester == null)
            {
                return NotFound();
            }
            var dto = new SemesterReadDto
            {
                Id = updatedSemester.Id,
                Name = updatedSemester.Name,
                AcademicYearId = updatedSemester.AcademicYearId,
                StartDate = updatedSemester.StartDate,
                EndDate = updatedSemester.EndDate,
                DeletedAt = updatedSemester.DeletedAt
            };
            return Ok(dto);
        }

        // POST: api/Semesters
        [HttpPost]
        public async Task<ActionResult<SemesterReadDto>> PostSemester(SemesterCreateDto semesterDto)
        {
            try
            {
                if (semesterDto == null) return BadRequest(new { message = "Dữ liệu học kỳ không hợp lệ" });
                if (string.IsNullOrWhiteSpace(semesterDto.Name)) return BadRequest(new { message = "Tên học kỳ không được để trống" });
                
                var academicYear = await _context.AcademicYears.FindAsync(semesterDto.AcademicYearId);
                if (academicYear == null) return BadRequest(new { message = "Năm học không tồn tại" });

                var existingSemester = await _context.Semesters
                    .AnyAsync(s => s.Name == semesterDto.Name && s.AcademicYearId == semesterDto.AcademicYearId && s.DeletedAt == null);
                if (existingSemester) return BadRequest(new { message = "Học kỳ đã tồn tại trong năm học này" });

                var semester = new Semester
                {
                    Name = semesterDto.Name,
                    AcademicYearId = semesterDto.AcademicYearId,
                    StartDate = semesterDto.StartDate,
                    EndDate = semesterDto.EndDate
                };

                _context.Semesters.Add(semester);
                await _context.SaveChangesAsync();

                var createdSemester = await _context.Semesters
                    .Include(s => s.AcademicYear)
                    .FirstOrDefaultAsync(s => s.Id == semester.Id);
                if (createdSemester == null)
                {
                    return StatusCode(500, new { message = "Lỗi khi tạo mới học kỳ" });
                }
                var dto = new SemesterReadDto
                {
                    Id = createdSemester.Id,
                    Name = createdSemester.Name,
                    AcademicYearId = createdSemester.AcademicYearId,
                    StartDate = createdSemester.StartDate,
                    EndDate = createdSemester.EndDate,
                    DeletedAt = createdSemester.DeletedAt
                };
                return CreatedAtAction(nameof(GetSemester), new { id = semester.Id }, dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo mới học kỳ", details = ex.Message });
            }
        }
        
        // SOFT DELETE: api/Semesters/soft-delete/5
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteSemester(int id)
        {
            try
            {
                var semester = await _context.Semesters.FindAsync(id);
                if (semester == null) return NotFound();
                if (semester.DeletedAt != null) return BadRequest("Học kỳ đã bị xóa mềm.");
                
                semester.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa mềm học kỳ");
            }
        }
        
        // BULK SOFT DELETE: api/Semesters/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách id không hợp lệ.");
            
            try
            {
                var semesters = await _context.Semesters.Where(d => ids.Contains(d.Id) && d.DeletedAt == null).ToListAsync();
                if (semesters.Count == 0) return NotFound("Không tìm thấy học kỳ nào để xóa mềm.");
                
                foreach (var item in semesters)
                {
                    item.DeletedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return Ok(new { softDeleted = semesters.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa mềm nhiều học kỳ: {ex.Message}");
            }
        }
        
        // PERMANENT DELETE: api/Semesters/permanent-delete/5
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteSemester(int id)
        {
            try
            {
                var semester = await _context.Semesters.FindAsync(id);
                if (semester == null) return NotFound();
                
                _context.Semesters.Remove(semester);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa vĩnh viễn học kỳ");
            }
        }
        
        // BULK PERMANENT DELETE: api/Semesters/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách id không hợp lệ.");
            
            try
            {
                var semesters = await _context.Semesters.Where(d => ids.Contains(d.Id)).ToListAsync();
                if (semesters.Count == 0) return NotFound("Không tìm thấy học kỳ nào để xóa vĩnh viễn.");
                
                _context.Semesters.RemoveRange(semesters);
                await _context.SaveChangesAsync();
                return Ok(new { permanentlyDeleted = semesters.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa vĩnh viễn nhiều học kỳ: {ex.Message}");
            }
        }

        // BULK RESTORE: api/Semesters/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách id không hợp lệ.");
            
            try
            {
                var semesters = await _context.Semesters.Where(d => ids.Contains(d.Id) && d.DeletedAt != null).ToListAsync();
                if (semesters.Count == 0) return NotFound("Không tìm thấy học kỳ nào để khôi phục.");
                
                foreach (var item in semesters)
                {
                    item.DeletedAt = null;
                }
                await _context.SaveChangesAsync();
                return Ok(new { restored = semesters.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi khôi phục nhiều học kỳ: {ex.Message}");
            }
        }

        // GET: api/Semesters/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedSemesters([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Semesters
                    .IgnoreQueryFilters() // Bỏ qua bộ lọc global nếu có
                    .Where(s => s.DeletedAt != null);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => s.Name.Contains(search) || (s.AcademicYear != null && s.AcademicYear.Name.Contains(search)));
                }
                
                var totalCount = await query.CountAsync();

                var semesters = await query
                    .Include(s => s.AcademicYear) // Chuyển Include xuống đây
                    .OrderByDescending(s => s.DeletedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(s => new SemesterReadDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        AcademicYearId = s.AcademicYearId,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        DeletedAt = s.DeletedAt
                    })
                    .ToListAsync();
                
                return Ok(new
                {
                    data = semesters,
                    total = totalCount,
                    page = page,
                    limit = limit
                });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }
        
        private bool SemesterExists(int id)
        {
            return _context.Semesters.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}