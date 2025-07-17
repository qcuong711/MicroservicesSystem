using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Lecturer;
using DataManagementApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DataManagementApi.Controllers
{
    [Route("api/lecturers")]
    [ApiController]
    public class LecturersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DepartmentAccessService _departmentAccessService;

        public LecturersController(ApplicationDbContext context, DepartmentAccessService departmentAccessService)
        {
            _context = context;
            _departmentAccessService = departmentAccessService;
        }

        // GET: api/Lecturers
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<object>> GetLecturers([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                // Lấy danh sách phòng ban có thể truy cập
                var accessibleDepartmentIds = await _departmentAccessService.GetAccessibleDepartmentIds(User);
                
                IQueryable<Lecturer> query = _context.Lecturers
                    .Include(l => l.Department)
                    .Where(l => l.DeletedAt == null)
                    .Where(l => !l.DepartmentId.HasValue || accessibleDepartmentIds.Contains(l.DepartmentId.Value));

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(l => l.Name.Contains(search) || l.Email.Contains(search) || (l.Specialization != null && l.Specialization.Contains(search)));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var lecturers = await query
                    .OrderBy(l => l.Name)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                var lecturerDtos = lecturers.Select(l => new LecturerReadDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Email = l.Email,
                    PhoneNumber = l.PhoneNumber,
                    DepartmentId = l.DepartmentId,
                    DepartmentName = l.Department?.Name,
                    AcademicRank = l.AcademicRank,
                    Degree = l.Degree,
                    Specialization = l.Specialization,
                    AvatarUrl = l.AvatarUrl,
                    IsActive = l.IsActive,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt,
                    DeletedAt = l.DeletedAt
                }).ToList();

                // Return paginated response format
                return Ok(new
                {
                    data = lecturerDtos,
                    total = totalCount,
                    page = page,
                    limit = limit
                });
            }
            catch (Exception ex)
            {
                // Log the exception details here if you have a logger
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi truy xuất dữ liệu từ cơ sở dữ liệu: {ex.Message}");
            }
        }

        // GET: api/Lecturers/all
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<LecturerReadDto>>> GetAllLecturers()
        {
            try
            {
                var lecturers = await _context.Lecturers
                    .Where(d => d.DeletedAt == null)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                var lecturerDtos = lecturers.Select(l => new LecturerReadDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Email = l.Email,
                    PhoneNumber = l.PhoneNumber,
                    DepartmentId = l.DepartmentId,
                    DepartmentName = l.Department?.Name,
                    AcademicRank = l.AcademicRank,
                    Degree = l.Degree,
                    Specialization = l.Specialization,
                    AvatarUrl = l.AvatarUrl,
                    IsActive = l.IsActive,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt,
                    DeletedAt = l.DeletedAt
                }).ToList();
                return lecturerDtos;
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }
        
        // GET: api/Lecturers/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<LecturerReadDto>> GetLecturer(int id)
        {
            try
            {
                var lecturer = await _context.Lecturers
                    .Include(l => l.Department)
                    .FirstOrDefaultAsync(l => l.Id == id && l.DeletedAt == null);

                if (lecturer == null)
                {
                    return NotFound();
                }

                var lecturerDto = new LecturerReadDto
                {
                    Id = lecturer.Id,
                    Name = lecturer.Name,
                    Email = lecturer.Email,
                    PhoneNumber = lecturer.PhoneNumber,
                    DepartmentId = lecturer.DepartmentId,
                    DepartmentName = lecturer.Department?.Name,
                    AcademicRank = lecturer.AcademicRank,
                    Degree = lecturer.Degree,
                    Specialization = lecturer.Specialization,
                    AvatarUrl = lecturer.AvatarUrl,
                    IsActive = lecturer.IsActive,
                    CreatedAt = lecturer.CreatedAt,
                    UpdatedAt = lecturer.UpdatedAt,
                    DeletedAt = lecturer.DeletedAt
                };

                return lecturerDto;
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // PUT: api/Lecturers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLecturer(int id, LecturerUpdateDto lecturerDto)
        {
            var existingLecturer = await _context.Lecturers.FindAsync(id);
            if (existingLecturer == null)
            {
                return NotFound();
            }

            if (await _context.Lecturers.AnyAsync(l => l.Email == lecturerDto.Email && l.Id != id))
            {
                return BadRequest(new { message = "Email already exists" });
            }
            existingLecturer.Name = lecturerDto.Name;
            existingLecturer.Email = lecturerDto.Email;
            existingLecturer.PhoneNumber = lecturerDto.PhoneNumber;
            existingLecturer.DepartmentId = lecturerDto.DepartmentId;
            existingLecturer.AcademicRank = lecturerDto.AcademicRank;
            existingLecturer.Degree = lecturerDto.Degree;
            existingLecturer.Specialization = lecturerDto.Specialization;
            existingLecturer.AvatarUrl = lecturerDto.AvatarUrl;
            existingLecturer.IsActive = lecturerDto.IsActive;
            existingLecturer.UpdatedAt = DateTime.UtcNow;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LecturerExists(id))
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
            var updatedLecturer = await _context.Lecturers.Include(l => l.Department).FirstOrDefaultAsync(l => l.Id == id);
            var updatedLecturerDto = new LecturerReadDto
            {
                Id = updatedLecturer.Id,
                Name = updatedLecturer.Name,
                Email = updatedLecturer.Email,
                PhoneNumber = updatedLecturer.PhoneNumber,
                DepartmentId = updatedLecturer.DepartmentId,
                DepartmentName = updatedLecturer.Department?.Name,
                AcademicRank = updatedLecturer.AcademicRank,
                Degree = updatedLecturer.Degree,
                Specialization = updatedLecturer.Specialization,
                AvatarUrl = updatedLecturer.AvatarUrl,
                IsActive = updatedLecturer.IsActive,
                CreatedAt = updatedLecturer.CreatedAt,
                UpdatedAt = updatedLecturer.UpdatedAt,
                DeletedAt = updatedLecturer.DeletedAt
            };
            return Ok(updatedLecturerDto);
        }

        // POST: api/Lecturers
        [HttpPost]
        public async Task<ActionResult<LecturerReadDto>> PostLecturer(LecturerCreateDto lecturerDto)
        {
            try
            {
                if (await _context.Lecturers.AnyAsync(l => l.Email == lecturerDto.Email))
                {
                    return BadRequest("Email đã tồn tại.");
                }
                var lecturer = new Lecturer
                {
                    Name = lecturerDto.Name,
                    Email = lecturerDto.Email,
                    PhoneNumber = lecturerDto.PhoneNumber,
                    DepartmentId = lecturerDto.DepartmentId,
                    AcademicRank = lecturerDto.AcademicRank,
                    Degree = lecturerDto.Degree,
                    Specialization = lecturerDto.Specialization,
                    AvatarUrl = lecturerDto.AvatarUrl,
                    IsActive = lecturerDto.IsActive,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Lecturers.Add(lecturer);
                await _context.SaveChangesAsync();
                var lecturerReadDto = new LecturerReadDto
                {
                    Id = lecturer.Id,
                    Name = lecturer.Name,
                    Email = lecturer.Email,
                    PhoneNumber = lecturer.PhoneNumber,
                    DepartmentId = lecturer.DepartmentId,
                    DepartmentName = lecturer.Department?.Name,
                    AcademicRank = lecturer.AcademicRank,
                    Degree = lecturer.Degree,
                    Specialization = lecturer.Specialization,
                    AvatarUrl = lecturer.AvatarUrl,
                    IsActive = lecturer.IsActive,
                    CreatedAt = lecturer.CreatedAt,
                    UpdatedAt = lecturer.UpdatedAt,
                    DeletedAt = lecturer.DeletedAt
                };
                return CreatedAtAction(nameof(GetLecturer), new { id = lecturer.Id }, lecturerReadDto);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi tạo mới giảng viên: {ex.Message}");
            }
        }
        
        // SOFT DELETE: api/Lecturers/soft-delete/5
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteLecturer(int id)
        {
            try
            {
                var lecturer = await _context.Lecturers.FindAsync(id);
                if (lecturer == null)
                {
                    return NotFound();
                }
                if (lecturer.DeletedAt != null)
                {
                    return BadRequest("Giảng viên đã bị xóa mềm.");
                }
                lecturer.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa mềm giảng viên");
            }
        }

        // BULK SOFT DELETE: api/Lecturers/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách id không hợp lệ.");
            try
            {
                var lecturers = await _context.Lecturers.Where(d => ids.Contains(d.Id) && d.DeletedAt == null).ToListAsync();
                if (lecturers.Count == 0)
                    return NotFound("Không tìm thấy giảng viên nào để xóa mềm.");
                foreach (var lecturer in lecturers)
                {
                    lecturer.DeletedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return Ok(new { softDeleted = lecturers.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa mềm nhiều giảng viên: {ex.Message}");
            }
        }
        
        // PERMANENT DELETE: api/Lecturers/permanent-delete/5
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteLecturer(int id)
        {
            try
            {
                var lecturer = await _context.Lecturers.FindAsync(id);
                if (lecturer == null)
                {
                    return NotFound();
                }
                _context.Lecturers.Remove(lecturer);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa vĩnh viễn giảng viên");
            }
        }

        // BULK PERMANENT DELETE: api/Lecturers/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách id không hợp lệ.");
            try
            {
                var lecturers = await _context.Lecturers.Where(d => ids.Contains(d.Id)).ToListAsync();
                if (lecturers.Count == 0)
                    return NotFound("Không tìm thấy giảng viên nào để xóa vĩnh viễn.");
                _context.Lecturers.RemoveRange(lecturers);
                await _context.SaveChangesAsync();
                return Ok(new { permanentlyDeleted = lecturers.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa vĩnh viễn nhiều giảng viên: {ex.Message}");
            }
        }

        // BULK RESTORE: api/Lecturers/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách id không hợp lệ.");
            try
            {
                var lecturers = await _context.Lecturers.Where(d => ids.Contains(d.Id) && d.DeletedAt != null).ToListAsync();
                if (lecturers.Count == 0)
                    return NotFound("Không tìm thấy giảng viên nào để khôi phục.");
                foreach (var lecturer in lecturers)
                {
                    lecturer.DeletedAt = null;
                }
                await _context.SaveChangesAsync();
                return Ok(new { restored = lecturers.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi khôi phục nhiều giảng viên: {ex.Message}");
            }
        }

        // GET: api/Lecturers/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedLecturers([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Lecturers
                    .Include(l => l.Department)
                    .Where(d => d.DeletedAt != null);
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(l => l.Name.Contains(search) || l.Email.Contains(search) || (l.Specialization != null && l.Specialization.Contains(search)));
                }
                var totalCount = await query.CountAsync();
                var lecturers = await query
                    .OrderBy(d => d.Name)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();
                var lecturerDtos = lecturers.Select(l => new LecturerReadDto
                {
                    Id = l.Id,
                    Name = l.Name,
                    Email = l.Email,
                    PhoneNumber = l.PhoneNumber,
                    DepartmentId = l.DepartmentId,
                    DepartmentName = l.Department?.Name,
                    AcademicRank = l.AcademicRank,
                    Degree = l.Degree,
                    Specialization = l.Specialization,
                    AvatarUrl = l.AvatarUrl,
                    IsActive = l.IsActive,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt,
                    DeletedAt = l.DeletedAt
                }).ToList();
                return Ok(new
                {
                    data = lecturerDtos,
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

        private bool LecturerExists(int id)
        {
            return _context.Lecturers.Any(e => e.Id == id);
        }
    }
}