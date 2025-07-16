using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Department;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DepartmentReadDto = DataManagementApi.Models.Dtos.Department.DepartmentReadDto;
using DepartmentUpdateDto = DataManagementApi.Models.Dtos.Department.DepartmentUpdateDto;
using DepartmentCreateDto = DataManagementApi.Models.Dtos.Department.DepartmentCreateDto;

namespace DataManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DepartmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Departments
        [HttpGet]
        public async Task<ActionResult<object>> GetDepartments([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                IQueryable<Department> query = _context.Departments
                    .Where(d => d.ParentDepartmentId == null && d.DeletedAt == null)
                    .Include(d => d.ChildDepartments);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(d => d.Name.Contains(search) || d.Code.Contains(search));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination - but for tree structure, we might want to return all for now
                // since pagination with tree structure is complex
                var departments = await query
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                // Filter child departments to exclude deleted ones
                foreach (var dept in departments)
                {
                    if (dept.ChildDepartments != null)
                    {
                        dept.ChildDepartments = dept.ChildDepartments.Where(cd => cd.DeletedAt == null).ToList();
                    }
                }

                // Return paginated response format
                return Ok(new
                {
                    data = departments,
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

        // GET: api/Departments/all
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<Department>>> GetAllDepartments()
        {
            try
            {
                // Lấy tất cả departments dạng flat list
                return await _context.Departments
                    .Where(d => d.DeletedAt == null)
                    .OrderBy(d => d.Name)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // GET: api/Departments/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DepartmentReadDto>> GetDepartment(int id)
        {
            try
            {
                var department = await _context.Departments
                    .Include(d => d.ParentDepartment)
                    .Include(d => d.ChildDepartments)
                    .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);

                if (department == null)
                {
                    return NotFound();
                }

                var dto = new DepartmentReadDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    Code = department.Code,
                    ParentDepartmentId = department.ParentDepartmentId,
                    ParentDepartmentName = department.ParentDepartment?.Name,
                    DeletedAt = department.DeletedAt,
                    ChildDepartments = department.ChildDepartments
                        .Where(cd => cd.DeletedAt == null)
                        .Select(cd => new DepartmentChildDto
                        {
                            Id = cd.Id,
                            Name = cd.Name,
                            Code = cd.Code,
                            ParentDepartmentId = cd.ParentDepartmentId,
                            DeletedAt = cd.DeletedAt
                        }).ToList()
                };

                return dto;
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // PUT: api/Departments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDepartment(int id, DepartmentUpdateDto departmentDto)
        {
            var existingDepartment = await _context.Departments.FindAsync(id);

            if (existingDepartment == null || existingDepartment.DeletedAt != null)
            {
                return NotFound();
            }

            // Optional: Validate ParentDepartmentId if it's provided
            if (departmentDto.ParentDepartmentId.HasValue)
            {
                // Prevent making a department a child of itself
                if (departmentDto.ParentDepartmentId.Value == id)
                {
                    return BadRequest("Một đơn vị không thể là đơn vị con của chính nó.");
                }

                var parentExists = await _context.Departments.AnyAsync(d => d.Id == departmentDto.ParentDepartmentId.Value && d.DeletedAt == null);
                if (!parentExists)
                {
                    return BadRequest("Đơn vị cha không tồn tại.");
                }
            }

            existingDepartment.Name = departmentDto.Name;
            existingDepartment.Code = departmentDto.Code;
            existingDepartment.ParentDepartmentId = departmentDto.ParentDepartmentId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi cập nhật dữ liệu");
            }

            return NoContent();
        }

        // POST: api/Departments
        [HttpPost]
        public async Task<ActionResult<DepartmentReadDto>> PostDepartment(DepartmentCreateDto departmentDto)
        {
            try
            {
                var department = new Department
                {
                    Name = departmentDto.Name,
                    Code = departmentDto.Code,
                    ParentDepartmentId = departmentDto.ParentDepartmentId
                };
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                var dto = new DepartmentReadDto
                {
                    Id = department.Id,
                    Name = department.Name,
                    Code = department.Code,
                    ParentDepartmentId = department.ParentDepartmentId,
                    DeletedAt = department.DeletedAt,
                    ChildDepartments = new List<DepartmentChildDto>()
                };

                return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, dto);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi tạo mới đơn vị");
            }
        }

        // SOFT DELETE: api/Departments/soft-delete/5
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                {
                    return NotFound();
                }
                if (department.DeletedAt != null)
                {
                    return BadRequest("Đơn vị đã bị xóa mềm.");
                }
                department.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa mềm đơn vị");
            }
        }

        // BULK SOFT DELETE: api/Departments/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách id không hợp lệ.");
            try
            {
                var departments = await _context.Departments.Where(d => ids.Contains(d.Id) && d.DeletedAt == null).ToListAsync();
                if (departments.Count == 0)
                    return NotFound("Không tìm thấy đơn vị nào để xóa mềm.");
                foreach (var dept in departments)
                {
                    dept.DeletedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return Ok(new { softDeleted = departments.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa mềm nhiều đơn vị: {ex.Message}");
            }
        }

        // PERMANENT DELETE: api/Departments/permanent-delete/5
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteDepartment(int id)
        {
            try
            {
                var department = await _context.Departments.FindAsync(id);
                if (department == null)
                {
                    return NotFound();
                }
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa vĩnh viễn đơn vị");
            }
        }

        // BULK PERMANENT DELETE: api/Departments/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách id không hợp lệ.");
            try
            {
                var departments = await _context.Departments.Where(d => ids.Contains(d.Id)).ToListAsync();
                if (departments.Count == 0)
                    return NotFound("Không tìm thấy đơn vị nào để xóa vĩnh viễn.");
                _context.Departments.RemoveRange(departments);
                await _context.SaveChangesAsync();
                return Ok(new { permanentlyDeleted = departments.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi xóa vĩnh viễn nhiều đơn vị: {ex.Message}");
            }
        }

        // BULK RESTORE: api/Departments/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest("Danh sách id không hợp lệ.");
            try
            {
                var departments = await _context.Departments.Where(d => ids.Contains(d.Id) && d.DeletedAt != null).ToListAsync();
                if (departments.Count == 0)
                    return NotFound("Không tìm thấy đơn vị nào để khôi phục.");
                foreach (var dept in departments)
                {
                    dept.DeletedAt = null;
                }
                await _context.SaveChangesAsync();
                return Ok(new { restored = departments.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi khôi phục nhiều đơn vị: {ex.Message}");
            }
        }

        // GET: api/Departments/list
        [HttpGet("list")]
        public async Task<ActionResult<object>> GetDepartmentList([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Departments
                    .Where(d => d.DeletedAt == null);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(d => d.Name.Contains(search) || d.Code.Contains(search));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var departments = await query
                    .OrderBy(d => d.Name)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                // Return paginated response format
                return Ok(new
                {
                    data = departments,
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

        // GET: api/Departments/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedDepartments([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Departments
                    .Where(d => d.DeletedAt != null);

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(d => d.Name.Contains(search) || d.Code.Contains(search));
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var departments = await query
                    .OrderBy(d => d.Name)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                // Return paginated response format
                return Ok(new
                {
                    data = departments,
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

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}