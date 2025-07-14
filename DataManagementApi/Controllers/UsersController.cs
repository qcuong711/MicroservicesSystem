using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<object>> GetUsers(
            [FromQuery] int page = 1, 
            [FromQuery] int limit = 10, 
            [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Users
                    .Where(u => u.DeletedAt == null)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .AsQueryable();
                
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
                }

                var totalCount = await query.CountAsync();

                var users = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        KeycloakUserId = u.KeycloakUserId,
                        Name = u.Name,
                        Email = u.Email,
                        AvatarUrl = u.AvatarUrl,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                        UserRoles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                    })
                    .ToListAsync();

                return Ok(new 
                {
                    data = users,
                    total = totalCount,
                    page,
                    limit
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi truy xuất dữ liệu: {ex.Message}");
            }
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && u.DeletedAt == null)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        KeycloakUserId = u.KeycloakUserId,
                        Name = u.Name,
                        Email = u.Email,
                        AvatarUrl = u.AvatarUrl,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                        UserRoles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound();
                }

                return user;
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }
        
        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<UserDto>> PostUser(CreateUserRequest request)
        {
            try
            {
                // Check model validation
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => new { 
                            Field = x.Key, 
                            Errors = x.Value?.Errors.Select(e => e.ErrorMessage) ?? new List<string>()
                        })
                        .ToList();
                    
                    return BadRequest(new { message = "Dữ liệu không hợp lệ", errors });
                }

                // Check if email already exists
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
                
                if (existingUserByEmail != null)
                {
                    return Conflict(new { message = "Email đã tồn tại trong hệ thống" });
                }

                // Check if Keycloak User ID already exists
                var existingUserByKeycloakId = await _context.Users
                    .FirstOrDefaultAsync(u => u.KeycloakUserId == request.KeycloakUserId);
                
                if (existingUserByKeycloakId != null)
                {
                    return Conflict(new { message = "Keycloak User ID đã tồn tại trong hệ thống" });
                }

                // Validate role IDs exist
                if (request.RoleIds.Any())
                {
                    var existingRoles = await _context.Roles
                        .Where(r => request.RoleIds.Contains(r.Id))
                        .CountAsync();
                    
                    if (existingRoles != request.RoleIds.Count)
                    {
                        return BadRequest(new { message = "Một hoặc nhiều vai trò được chỉ định không tồn tại" });
                    }
                }

                var user = new User
                {
                    KeycloakUserId = request.KeycloakUserId,
                    Name = request.Name,
                    Email = request.Email,
                    AvatarUrl = request.AvatarUrl,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Add roles if provided
                if (request.RoleIds.Any())
                {
                    var userRoles = request.RoleIds.Select(roleId => new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId
                    }).ToList();

                    _context.UserRoles.AddRange(userRoles);
                    await _context.SaveChangesAsync();
                }

                // Fetch the created user with roles
                var createdUser = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                if (createdUser == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi tạo mới người dùng");
                }

                var userDto = new UserDto
                {
                    Id = createdUser.Id,
                    KeycloakUserId = createdUser.KeycloakUserId,
                    Name = createdUser.Name,
                    Email = createdUser.Email,
                    AvatarUrl = createdUser.AvatarUrl,
                    IsActive = createdUser.IsActive,
                    CreatedAt = createdUser.CreatedAt,
                    UpdatedAt = createdUser.UpdatedAt,
                    UserRoles = createdUser.UserRoles.Select(ur => ur.Role.Name).ToList()
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
            }
            catch (Exception ex)
            {
                // Log the actual exception for debugging
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Lỗi khi tạo mới người dùng", details = ex.Message });
            }
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UpdateUserRequest request)
        {
            try
            {
                // Check model validation
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => new { 
                            Field = x.Key, 
                            Errors = x.Value?.Errors.Select(e => e.ErrorMessage) ?? new List<string>()
                        })
                        .ToList();
                    
                    return BadRequest(new { message = "Dữ liệu không hợp lệ", errors });
                }

                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null || user.DeletedAt != null)
                {
                    return NotFound(new { message = "Người dùng không tồn tại" });
                }

                // Validate email if being updated
                if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
                {
                    var existingUserWithEmail = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != id);
                    
                    if (existingUserWithEmail != null)
                    {
                        return Conflict(new { message = "Email đã được sử dụng bởi người dùng khác" });
                    }
                }

                // Validate role IDs exist if being updated
                if (request.RoleIds != null && request.RoleIds.Any())
                {
                    var existingRoles = await _context.Roles
                        .Where(r => request.RoleIds.Contains(r.Id))
                        .CountAsync();
                    
                    if (existingRoles != request.RoleIds.Count)
                    {
                        return BadRequest(new { message = "Một hoặc nhiều vai trò được chỉ định không tồn tại" });
                    }
                }

                // Update user properties
                if (!string.IsNullOrWhiteSpace(request.Name)) user.Name = request.Name;
                if (!string.IsNullOrWhiteSpace(request.Email)) user.Email = request.Email;
                if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;
                if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
                user.UpdatedAt = DateTime.UtcNow;

                // Update roles if provided
                if (request.RoleIds != null)
                {
                    // Remove existing roles
                    _context.UserRoles.RemoveRange(user.UserRoles);
                    
                    // Add new roles
                    if (request.RoleIds.Any())
                    {
                        var newUserRoles = request.RoleIds.Select(roleId => new UserRole
                        {
                            UserId = id,
                            RoleId = roleId
                        }).ToList();

                        _context.UserRoles.AddRange(newUserRoles);
                    }
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound(new { message = "Người dùng không tồn tại" });
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Lỗi cập nhật dữ liệu", details = ex.Message });
            }
        }
        
        // SOFT DELETE: api/users/soft-delete/5
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (user.DeletedAt != null) return BadRequest("Người dùng đã được xóa.");

            user.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/users/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedUsers([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.Users
                .Where(u => u.DeletedAt != null)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.DeletedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(u => new UserDto 
                {
                    Id = u.Id,
                    KeycloakUserId = u.KeycloakUserId,
                    Name = u.Name,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    DeletedAt = u.DeletedAt, // Include DeletedAt for deleted view
                    UserRoles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();
            
            return Ok(new { data = users, total = totalCount, page, limit });
        }


        // BULK SOFT DELETE: api/users/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");

            var users = await _context.Users.Where(u => ids.Contains(u.Id) && u.DeletedAt == null).ToListAsync();
            if (users.Count == 0) return NotFound("Không tìm thấy người dùng hợp lệ để xóa.");

            foreach (var user in users)
            {
                user.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa thành công {users.Count} người dùng."});
        }
        
        // BULK RESTORE: api/users/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            
            var users = await _context.Users.Where(u => ids.Contains(u.Id) && u.DeletedAt != null).ToListAsync();
            if (users.Count == 0) return NotFound("Không tìm thấy người dùng hợp lệ để khôi phục.");
            
            foreach (var user in users)
            {
                user.DeletedAt = null; // Restore by setting DeletedAt to null
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã khôi phục thành công {users.Count} người dùng."});
        }

        // BULK PERMANENT DELETE: api/users/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
             if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");

            var users = await _context.Users
                .Include(u => u.UserRoles)
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            if (users.Count == 0) return NotFound("Không tìm thấy người dùng hợp lệ để xóa vĩnh viễn.");

             // Note: You should consider what to do in Keycloak as well. 
             // This implementation only removes from the local DB.
            _context.Users.RemoveRange(users);
            
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa vĩnh viễn {users.Count} người dùng." });
        }


        // This replaces the old DELETE endpoint and should be used with caution.
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteUser(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
            
            if (user == null)
            {
                return NotFound(new { message = "Người dùng không tồn tại" });
            }

             // Note: You should consider what to do in Keycloak as well.
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Users/{userId}/roles
        [HttpPost("{userId}/roles")]
        public async Task<IActionResult> AssignRoleToUser(int userId, [FromBody] UserRoleDto userRoleDto)
        {
            if (userRoleDto == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return NotFound($"Không tìm thấy người dùng với ID {userId}.");
            }

            var roleExists = await _context.Roles.AnyAsync(r => r.Id == userRoleDto.RoleId);
            if (!roleExists)
            {
                return NotFound($"Không tìm thấy vai trò với ID {userRoleDto.RoleId}.");
            }

            var userRole = new UserRole
            {
                UserId = userId,
                RoleId = userRoleDto.RoleId
            };

            var alreadyExists = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == userRoleDto.RoleId);

            if (alreadyExists)
            {
                return Conflict("Người dùng đã có vai trò này.");
            }

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gán vai trò thành công." });
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
} 