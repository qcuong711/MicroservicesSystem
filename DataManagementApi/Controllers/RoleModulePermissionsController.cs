using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.RoleModulePermission;
using DataManagementApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DataManagementApi.Controllers
{
    [Route("api/role-module-permissions")]
    [ApiController]
    public class RoleModulePermissionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoleModulePermissionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/role-module-permissions/matrix/{roleId}
        [HttpGet("matrix/{roleId}")]
        public async Task<ActionResult<RolePermissionMatrixDto>> GetRolePermissionMatrix(int roleId)
        {
            try
            {
                var role = await _context.Roles
                    .Where(r => r.Id == roleId && r.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (role == null)
                {
                    return NotFound($"Role với ID {roleId} không tồn tại");
                }

                // Lấy tất cả modules có trong hệ thống từ ModuleRegistry
                var allModules = ModuleRegistry.GetModuleNames();

                // Lấy permissions hiện tại của role
                var existingPermissions = await _context.RoleModulePermissions
                    .Where(rmp => rmp.RoleId == roleId && rmp.DeletedAt == null)
                    .ToListAsync();

                // Tạo matrix đầy đủ (bao gồm cả modules chưa có permission)
                var modules = allModules.Select(moduleName =>
                {
                    var existingPerm = existingPermissions.FirstOrDefault(p => p.ModuleName == moduleName);
                    return new ModulePermissionDto
                    {
                        ModuleName = moduleName,
                        CanCreate = existingPerm?.CanCreate ?? false,
                        CanRead = existingPerm?.CanRead ?? false,
                        CanUpdate = existingPerm?.CanUpdate ?? false,
                        CanDelete = existingPerm?.CanDelete ?? false
                    };
                }).OrderBy(m => m.ModuleName).ToList();

                var result = new RolePermissionMatrixDto
                {
                    RoleId = roleId,
                    RoleName = role.Name,
                    Modules = modules
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy matrix permissions: {ex.Message}");
            }
        }

        // PUT: api/role-module-permissions/matrix/{roleId}
        [HttpPut("matrix/{roleId}")]
        public async Task<IActionResult> UpdateRolePermissionMatrix(int roleId, UpdateRolePermissionMatrixDto dto)
        {
            try
            {
                if (roleId != dto.RoleId)
                {
                    return BadRequest("Role ID không khớp");
                }

                var role = await _context.Roles
                    .Where(r => r.Id == roleId && r.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (role == null)
                {
                    return NotFound($"Role với ID {roleId} không tồn tại");
                }

                // Xóa tất cả permissions cũ của role này
                var existingPermissions = await _context.RoleModulePermissions
                    .Where(rmp => rmp.RoleId == roleId)
                    .ToListAsync();

                _context.RoleModulePermissions.RemoveRange(existingPermissions);

                // Thêm permissions mới (chỉ thêm những modules có ít nhất 1 permission = true)
                var newPermissions = dto.Modules
                    .Where(m => m.CanCreate || m.CanRead || m.CanUpdate || m.CanDelete)
                    .Select(m => new Models.RoleModulePermission
                    {
                        RoleId = roleId,
                        ModuleName = m.ModuleName,
                        CanCreate = m.CanCreate,
                        CanRead = m.CanRead,
                        CanUpdate = m.CanUpdate,
                        CanDelete = m.CanDelete,
                        CreatedAt = DateTime.UtcNow
                    })
                    .ToList();

                if (newPermissions.Any())
                {
                    await _context.RoleModulePermissions.AddRangeAsync(newPermissions);
                }

                await _context.SaveChangesAsync();

                // Broadcast permission updates to affected users (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await BroadcastPermissionUpdatesToUsers(roleId);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail the main operation
                    }
                });

                return Ok(new { message = $"Cập nhật thành công matrix permissions cho role {role.Name}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi cập nhật matrix permissions: {ex.Message}");
            }
        }

        // GET: api/role-module-permissions/modules
        [HttpGet("modules")]
        public async Task<ActionResult<List<object>>> GetAvailableModules()
        {
            try
            {
                // Sử dụng ModuleRegistry để lấy danh sách modules
                var modules = ModuleRegistry.Modules.Values
                    .Where(m => m.IsActive)
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new
                    {
                        m.Name,
                        m.DisplayName,
                        m.Description,
                        m.Category,
                        AvailablePermissions = m.AvailablePermissions
                    })
                    .ToList();

                return Ok(modules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy danh sách modules: {ex.Message}");
            }
        }

        // GET: api/role-module-permissions/{roleId}
        [HttpGet("{roleId}")]
        public async Task<ActionResult<List<RoleModulePermissionDto>>> GetRoleModulePermissions(int roleId)
        {
            try
            {
                var permissions = await _context.RoleModulePermissions
                    .Where(rmp => rmp.RoleId == roleId && rmp.DeletedAt == null)
                    .Select(rmp => new RoleModulePermissionDto
                    {
                        Id = rmp.Id,
                        RoleId = rmp.RoleId,
                        ModuleName = rmp.ModuleName,
                        CanCreate = rmp.CanCreate,
                        CanRead = rmp.CanRead,
                        CanUpdate = rmp.CanUpdate,
                        CanDelete = rmp.CanDelete
                    })
                    .OrderBy(p => p.ModuleName)
                    .ToListAsync();

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy permissions: {ex.Message}");
            }
        }

        // POST: api/role-module-permissions
        [HttpPost]
        public async Task<ActionResult<RoleModulePermissionDto>> CreateRoleModulePermission(CreateRoleModulePermissionDto dto)
        {
            try
            {
                // Kiểm tra role tồn tại
                var roleExists = await _context.Roles
                    .AnyAsync(r => r.Id == dto.RoleId && r.DeletedAt == null);

                if (!roleExists)
                {
                    return BadRequest($"Role với ID {dto.RoleId} không tồn tại");
                }

                // Kiểm tra đã tồn tại permission cho module này chưa
                var existingPermission = await _context.RoleModulePermissions
                    .FirstOrDefaultAsync(rmp => rmp.RoleId == dto.RoleId && rmp.ModuleName == dto.ModuleName);

                if (existingPermission != null)
                {
                    return BadRequest($"Permission cho module {dto.ModuleName} đã tồn tại cho role này");
                }

                var newPermission = new Models.RoleModulePermission
                {
                    RoleId = dto.RoleId,
                    ModuleName = dto.ModuleName,
                    CanCreate = dto.CanCreate,
                    CanRead = dto.CanRead,
                    CanUpdate = dto.CanUpdate,
                    CanDelete = dto.CanDelete,
                    CreatedAt = DateTime.UtcNow
                };

                _context.RoleModulePermissions.Add(newPermission);
                await _context.SaveChangesAsync();

                var result = new RoleModulePermissionDto
                {
                    Id = newPermission.Id,
                    RoleId = newPermission.RoleId,
                    ModuleName = newPermission.ModuleName,
                    CanCreate = newPermission.CanCreate,
                    CanRead = newPermission.CanRead,
                    CanUpdate = newPermission.CanUpdate,
                    CanDelete = newPermission.CanDelete
                };

                return CreatedAtAction(nameof(GetRoleModulePermissions), new { roleId = dto.RoleId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi tạo permission: {ex.Message}");
            }
        }

        // DELETE: api/role-module-permissions/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoleModulePermission(int id)
        {
            try
            {
                var permission = await _context.RoleModulePermissions.FindAsync(id);
                
                if (permission == null)
                {
                    return NotFound();
                }

                _context.RoleModulePermissions.Remove(permission);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xóa permission: {ex.Message}");
            }
        }

        /// <summary>
        /// Broadcast permission updates to all users with the specified role
        /// </summary>
        private async Task BroadcastPermissionUpdatesToUsers(int roleId)
        {
            try
            {
                // Get all users with this role
                var usersWithRole = await _context.Users
                    .Include(u => u.UserRoles)
                    .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId) && u.DeletedAt == null)
                    .Select(u => u.KeycloakUserId)
                    .ToListAsync();

                // Broadcast to each user
                foreach (var keycloakUserId in usersWithRole)
                {
                    if (!string.IsNullOrEmpty(keycloakUserId))
                    {
                        await PermissionsStreamController.BroadcastRoleUpdate(keycloakUserId);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - this is fire-and-forget
            }
        }
    }
} 