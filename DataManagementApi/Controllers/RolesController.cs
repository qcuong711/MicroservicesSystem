using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Role;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RolesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<ActionResult<object>> GetRoles(
            [FromQuery] int page = 1, 
            [FromQuery] int limit = 10, 
            [FromQuery] string search = "")
        {
            var query = _context.Roles
                .Where(r => r.DeletedAt == null)
                .AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search)));
            }
            var totalCount = await query.CountAsync();
            var roles = await query
                .OrderBy(r => r.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(r => new RoleReadDto {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    DeletedAt = r.DeletedAt
                })
                .ToListAsync();
            return Ok(new 
            {
                data = roles,
                total = totalCount,
                page,
                limit
            });
        }

        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<RoleReadDto>>> GetAllRoles()
        {
            var roles = await _context.Roles
                .Where(r => r.DeletedAt == null)
                .OrderBy(r => r.Name)
                .Select(r => new RoleReadDto {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    DeletedAt = r.DeletedAt
                })
                .ToListAsync();
            return Ok(roles);
        }

        // GET: api/Roles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleReadDto>> GetRole(int id)
        {
            var r = await _context.Roles
                .Where(x => x.Id == id && x.DeletedAt == null)
                .FirstOrDefaultAsync();
            if (r == null) return NotFound();
            var dto = new RoleReadDto {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                DeletedAt = r.DeletedAt
            };
            return Ok(dto);
        }

        // POST: api/Roles
        [HttpPost]
        public async Task<ActionResult<RoleReadDto>> PostRole(RoleCreateDto roleDto)
        {
            var role = new Role
            {
                Name = roleDto.Name,
                Description = roleDto.Description,
                DeletedAt = null
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            var dto = new RoleReadDto {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                DeletedAt = role.DeletedAt
            };
            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, dto);
        }

        // PUT: api/Roles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRole(int id, RoleUpdateDto roleDto)
        {
            var existingRole = await _context.Roles.FindAsync(id);
            if (existingRole == null || existingRole.DeletedAt != null)
            {
                return NotFound("Vai trò không tồn tại hoặc đã bị xóa.");
            }
            if(roleDto.Name != null)
                existingRole.Name = roleDto.Name;
            if(roleDto.Description != null)
                existingRole.Description = roleDto.Description;
            await _context.SaveChangesAsync();
            var dto = new RoleReadDto {
                Id = existingRole.Id,
                Name = existingRole.Name,
                Description = existingRole.Description,
                DeletedAt = existingRole.DeletedAt
            };
            return Ok(dto);
        }
        
        // SOFT DELETE: api/roles/soft-delete/5
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return NotFound();
            if (role.DeletedAt != null) return BadRequest("Vai trò đã được xóa.");

            role.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        
        // GET: api/roles/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedRoles([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.Roles
                .Where(r => r.DeletedAt != null)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var roles = await query
                .OrderByDescending(r => r.DeletedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();
            
            return Ok(new { data = roles, total = totalCount, page, limit });
        }
        
        // BULK SOFT DELETE: api/roles/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");

            var roles = await _context.Roles.Where(r => ids.Contains(r.Id) && r.DeletedAt == null).ToListAsync();
            if (roles.Count == 0) return NotFound("Không tìm thấy vai trò hợp lệ để xóa.");

            foreach (var role in roles)
            {
                role.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa thành công {roles.Count} vai trò."});
        }
        
        // BULK RESTORE: api/roles/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            
            var roles = await _context.Roles.Where(r => ids.Contains(r.Id) && r.DeletedAt != null).ToListAsync();
            if (roles.Count == 0) return NotFound("Không tìm thấy vai trò hợp lệ để khôi phục.");
            
            foreach (var role in roles)
            {
                role.DeletedAt = null;
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã khôi phục thành công {roles.Count} vai trò."});
        }

        // BULK PERMANENT DELETE: api/roles/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");

            var roles = await _context.Roles
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            if (roles.Count == 0) return NotFound("Không tìm thấy vai trò hợp lệ để xóa vĩnh viễn.");

            _context.Roles.RemoveRange(roles);
            
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa vĩnh viễn {roles.Count} vai trò." });
        }

        // Replaces the old DELETE endpoint
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteRole(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RoleExists(int id)
        {
            return _context.Roles.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}