using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.AcademicYear;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/academic-years")]
    [ApiController]
    public class AcademicYearsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AcademicYearsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AcademicYears
        [HttpGet]
        public async Task<ActionResult<object>> GetAcademicYears(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string search = "")
        {
            try
            {
                var query = _context.AcademicYears.Where(ay => ay.DeletedAt == null);
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(y => y.Name.Contains(search));
                }
                var totalCount = await query.CountAsync();
                var years = await query
                    .OrderByDescending(y => y.StartDate)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(y => new AcademicYearReadDto {
                        Id = y.Id,
                        Name = y.Name,
                        StartDate = y.StartDate,
                        EndDate = y.EndDate,
                        CreatedAt = y.CreatedAt,
                        UpdatedAt = y.UpdatedAt,
                        DeletedAt = y.DeletedAt
                    })
                    .ToListAsync();
                return Ok(new
                {
                    data = years,
                    total = totalCount,
                    page = page,
                    limit = limit
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi truy xuất dữ liệu từ cơ sở dữ liệu: {ex.Message}");
            }
        }

        // GET: api/AcademicYears/all
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<AcademicYearReadDto>>> GetAllAcademicYears()
        {
            try
            {
                var years = await _context.AcademicYears
                    .Where(ay => ay.DeletedAt == null)
                    .OrderByDescending(y => y.StartDate)
                    .Select(y => new AcademicYearReadDto {
                        Id = y.Id,
                        Name = y.Name,
                        StartDate = y.StartDate,
                        EndDate = y.EndDate,
                        CreatedAt = y.CreatedAt,
                        UpdatedAt = y.UpdatedAt,
                        DeletedAt = y.DeletedAt
                    })
                    .ToListAsync();
                return Ok(years);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi truy xuất dữ liệu từ cơ sở dữ liệu: {ex.Message}");
            }
        }

        // GET: api/AcademicYears/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AcademicYearReadDto>> GetAcademicYear(int id)
        {
            try
            {
                var y = await _context.AcademicYears
                    .Where(ay => ay.Id == id && ay.DeletedAt == null)
                    .Select(ay => new AcademicYearReadDto {
                        Id = ay.Id,
                        Name = ay.Name,
                        StartDate = ay.StartDate,
                        EndDate = ay.EndDate,
                        CreatedAt = ay.CreatedAt,
                        UpdatedAt = ay.UpdatedAt,
                        DeletedAt = ay.DeletedAt
                    })
                    .FirstOrDefaultAsync();
                if (y == null)
                {
                    return NotFound("Không tìm thấy năm học");
                }
                return Ok(y);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi truy xuất dữ liệu từ cơ sở dữ liệu: {ex.Message}");
            }
        }

        // PUT: api/AcademicYears/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAcademicYear(int id, AcademicYearUpdateDto academicYearDto)
        {
            var existingYear = await _context.AcademicYears.FindAsync(id);
            if (existingYear == null || existingYear.DeletedAt != null)
            {
                 return NotFound("Không tìm thấy năm học");
            }
            if (string.IsNullOrWhiteSpace(academicYearDto.Name))
            {
                return BadRequest("Tên niên khóa không được để trống");
            }
            if (academicYearDto.StartDate >= academicYearDto.EndDate)
            {
                return BadRequest("Ngày bắt đầu phải trước ngày kết thúc");
            }
            var duplicateYear = await _context.AcademicYears
                .FirstOrDefaultAsync(y => y.Name == academicYearDto.Name && y.Id != id && y.DeletedAt == null);
            if (duplicateYear != null)
            {
                return BadRequest("Tên niên khóa đã tồn tại");
            }
            existingYear.Name = academicYearDto.Name;
            existingYear.StartDate = academicYearDto.StartDate;
            existingYear.EndDate = academicYearDto.EndDate;
            existingYear.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AcademicYearExists(id))
                {
                    return NotFound("Không tìm thấy năm học");
                }
                else
                {
                    throw;
                }
            }
            catch(Exception ex)
            {
                 return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi cập nhật dữ liệu: {ex.Message}");
            }
            var result = new AcademicYearReadDto {
                Id = existingYear.Id,
                Name = existingYear.Name,
                StartDate = existingYear.StartDate,
                EndDate = existingYear.EndDate,
                CreatedAt = existingYear.CreatedAt,
                UpdatedAt = existingYear.UpdatedAt,
                DeletedAt = existingYear.DeletedAt
            };
            return Ok(result);
        }

        // POST: api/AcademicYears
        [HttpPost]
        public async Task<ActionResult<AcademicYearReadDto>> PostAcademicYear(AcademicYearCreateDto academicYearDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(academicYearDto.Name))
                {
                    return BadRequest("Tên niên khóa không được để trống");
                }
                if (academicYearDto.StartDate >= academicYearDto.EndDate)
                {
                    return BadRequest("Ngày bắt đầu phải trước ngày kết thúc");
                }
                var existingYear = await _context.AcademicYears
                    .FirstOrDefaultAsync(y => y.Name == academicYearDto.Name && y.DeletedAt == null);
                if (existingYear != null)
                {
                    return BadRequest("Tên niên khóa đã tồn tại");
                }
                var academicYear = new AcademicYear
                {
                    Name = academicYearDto.Name,
                    StartDate = academicYearDto.StartDate,
                    EndDate = academicYearDto.EndDate,
                    CreatedAt = DateTime.UtcNow
                };
                _context.AcademicYears.Add(academicYear);
                await _context.SaveChangesAsync();
                var result = new AcademicYearReadDto {
                    Id = academicYear.Id,
                    Name = academicYear.Name,
                    StartDate = academicYear.StartDate,
                    EndDate = academicYear.EndDate,
                    CreatedAt = academicYear.CreatedAt,
                    UpdatedAt = academicYear.UpdatedAt,
                    DeletedAt = academicYear.DeletedAt
                };
                return CreatedAtAction(nameof(GetAcademicYear), new { id = academicYear.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi tạo mới năm học: {ex.Message}");
            }
        }

        // SOFT DELETE: api/AcademicYears/soft-delete/5
        [HttpPost("soft-delete/{id:int}")]
        public async Task<IActionResult> SoftDeleteAcademicYear(int id)
        {
            try
            {
                var academicYear = await _context.AcademicYears.FindAsync(id);
                if (academicYear == null)
                {
                    return NotFound("Không tìm thấy năm học");
                }
                if (academicYear.DeletedAt != null)
                {
                    return BadRequest("Năm học đã bị xóa.");
                }
                academicYear.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch(Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa năm học: {ex.Message}");
            }
        }
        
        // GET: api/AcademicYears/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedAcademicYears([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.AcademicYears.Where(ay => ay.DeletedAt != null);

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(y => y.Name.Contains(search));
                }

                var totalCount = await query.CountAsync();

                var years = await query
                    .OrderByDescending(y => y.DeletedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(y => new AcademicYearReadDto {
                        Id = y.Id,
                        Name = y.Name,
                        StartDate = y.StartDate,
                        EndDate = y.EndDate,
                        CreatedAt = y.CreatedAt,
                        UpdatedAt = y.UpdatedAt,
                        DeletedAt = y.DeletedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = years,
                    total = totalCount,
                    page = page,
                    limit = limit
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi truy xuất dữ liệu từ cơ sở dữ liệu: {ex.Message}");
            }
        }

        // BULK SOFT DELETE: api/AcademicYears/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách id không hợp lệ.");
            try
            {
                var years = await _context.AcademicYears.Where(d => ids.Contains(d.Id) && d.DeletedAt == null).ToListAsync();
                if (years.Count == 0)
                    return NotFound("Không tìm thấy năm học nào để xóa.");
                foreach (var year in years)
                {
                    year.DeletedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return Ok(new { softDeleted = years.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa nhiều năm học: {ex.Message}");
            }
        }

        // PERMANENT DELETE: api/AcademicYears/permanent-delete/5
        [HttpDelete("permanent-delete/{id:int}")]
        public async Task<IActionResult> PermanentDeleteAcademicYear(int id)
        {
            try
            {
                var year = await _context.AcademicYears.FindAsync(id);
                if (year == null)
                {
                    return NotFound();
                }
                _context.AcademicYears.Remove(year);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa vĩnh viễn năm học: {ex.Message}");
            }
        }

        // BULK PERMANENT DELETE: api/AcademicYears/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách id không hợp lệ.");
            try
            {
                var years = await _context.AcademicYears.Where(d => ids.Contains(d.Id)).ToListAsync();
                if (years.Count == 0)
                    return NotFound("Không tìm thấy năm học nào để xóa vĩnh viễn.");
                _context.AcademicYears.RemoveRange(years);
                await _context.SaveChangesAsync();
                return Ok(new { permanentlyDeleted = years.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa vĩnh viễn nhiều năm học: {ex.Message}");
            }
        }

        // BULK RESTORE: api/AcademicYears/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách id không hợp lệ.");
            try
            {
                var years = await _context.AcademicYears.Where(d => ids.Contains(d.Id) && d.DeletedAt != null).ToListAsync();
                if (years.Count == 0)
                    return NotFound("Không tìm thấy năm học nào để khôi phục.");
                foreach (var year in years)
                {
                    year.DeletedAt = null;
                }
                await _context.SaveChangesAsync();
                return Ok(new { restored = years.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi khôi phục nhiều năm học: {ex.Message}");
            }
        }

        private bool AcademicYearExists(int id)
        {
            return _context.AcademicYears.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}