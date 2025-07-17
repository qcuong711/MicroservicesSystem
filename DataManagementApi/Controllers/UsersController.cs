using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.User;
using DataManagementApi.Models.Dtos.UserRole;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
                    .Select(u => new UserReadDto
                    {
                        Id = u.Id,
                        KeycloakUserId = u.KeycloakUserId,
                        Name = u.Name,
                        Email = u.Email,
                        AvatarUrl = u.AvatarUrl,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                        DeletedAt = u.DeletedAt,
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
        public async Task<ActionResult<UserReadDto>> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && u.DeletedAt == null)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Select(u => new UserReadDto
                    {
                        Id = u.Id,
                        KeycloakUserId = u.KeycloakUserId,
                        Name = u.Name,
                        Email = u.Email,
                        AvatarUrl = u.AvatarUrl,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                        DeletedAt = u.DeletedAt
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
        public async Task<ActionResult<UserReadDto>> PostUser(CreateUserDto request)
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

                var userDto = new UserReadDto
                {
                    Id = createdUser.Id,
                    KeycloakUserId = createdUser.KeycloakUserId,
                    Name = createdUser.Name,
                    Email = createdUser.Email,
                    AvatarUrl = createdUser.AvatarUrl,
                    IsActive = createdUser.IsActive,
                    CreatedAt = createdUser.CreatedAt,
                    UpdatedAt = createdUser.UpdatedAt,
                    DeletedAt = createdUser.DeletedAt
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
        public async Task<IActionResult> PutUser(int id, UserUpdateDto request)
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
                user.IsActive = request.IsActive;
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
                .Select(u => new UserReadDto 
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
            if (ids == null || !ids.Any()) return BadRequest(new { message = "Danh sách ID không hợp lệ." });

            try
            {
                // Xóa UserRoles liên kết
                var userRoles = await _context.UserRoles.Where(ur => ids.Contains(ur.UserId)).ToListAsync();
                if (userRoles.Any())
                {
                    _context.UserRoles.RemoveRange(userRoles);
                }

                // Xóa Internships liên kết (StudentId)
                var internships = await _context.Internships.Where(i => ids.Contains(i.StudentId)).ToListAsync();
                if (internships.Any())
                {
                    _context.Internships.RemoveRange(internships);
                }

                // Xóa Theses liên kết (StudentId)
                var theses = await _context.Theses.Where(t => t.StudentId != null && ids.Contains((int)t.StudentId)).ToListAsync();
                if (theses.Any())
                {
                    _context.Theses.RemoveRange(theses);
                }

                // Có thể thêm các bảng liên kết khác ở đây nếu cần

                var users = await _context.Users
                    .Include(u => u.UserRoles)
                    .Where(u => ids.Contains(u.Id))
                    .ToListAsync();

                if (users.Count == 0) return NotFound(new { message = "Không tìm thấy người dùng hợp lệ để xóa vĩnh viễn." });

                _context.Users.RemoveRange(users);
                await _context.SaveChangesAsync();
                return Ok(new { message = $"Đã xóa vĩnh viễn {users.Count} người dùng và toàn bộ dữ liệu liên kết." });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Không thể xóa user vì còn dữ liệu liên kết khác (ví dụ: thực tập, luận văn, ...)", details = ex.Message });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi null rõ ràng
                if (ex is InvalidOperationException || ex is NullReferenceException || ex.Message.Contains("Data is Null"))
                {
                    return StatusCode(500, new { message = "Lỗi dữ liệu: Có thể có trường liên kết null hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra lại dữ liệu liên kết trước khi xóa.", details = ex.Message });
                }
                return StatusCode(500, new { message = "Lỗi hệ thống", details = ex.Message });
            }
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
        public async Task<IActionResult> AssignRoleToUser(int userId, [FromBody] DataManagementApi.Models.Dtos.UserRole.UserRoleDto userRoleDto)
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

        // GET: api/users/me
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserReadDto>> GetCurrentUser()
        {
            try
            {
                // Lấy Keycloak User ID từ JWT token
                var keycloakUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(keycloakUserId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ hoặc không chứa thông tin user." });
                }

                // Tìm user trong database theo KeycloakUserId
                var user = await _context.Users
                    .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Select(u => new UserReadDto
                    {
                        Id = u.Id,
                        KeycloakUserId = u.KeycloakUserId,
                        Name = u.Name,
                        Email = u.Email,
                        AvatarUrl = u.AvatarUrl,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt,
                        DeletedAt = u.DeletedAt,
                        UserRoles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "Không tìm thấy thông tin user trong hệ thống." });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi truy xuất dữ liệu: {ex.Message}");
            }
        }

        // GET: api/users/me/menus
        [HttpGet("me/menus")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetCurrentUserMenus()
        {
            try
            {
                // Lấy Keycloak User ID từ JWT token
                var keycloakUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(keycloakUserId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ hoặc không chứa thông tin user." });
                }

                // Tìm user trong database theo KeycloakUserId
                var user = await _context.Users
                    .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RoleMenus)
                    .ThenInclude(rm => rm.Menu)
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new { message = "Không tìm thấy thông tin user trong hệ thống." });
                }

                // Lấy tất cả menu mà user có quyền truy cập thông qua roles
                var accessibleMenuIds = user.UserRoles
                    .SelectMany(ur => ur.Role.RoleMenus)
                    .Select(rm => rm.MenuId)
                    .Distinct()
                    .ToList();

                // Nếu user không có quyền truy cập menu nào, trả về empty array
                if (!accessibleMenuIds.Any())
                {
                    return Ok(new List<object>());
                }

                // Lấy tất cả menu mà user có quyền truy cập (bao gồm cả parent menu)
                var accessibleMenus = await _context.Menus
                    .Where(m => m.DeletedAt == null && accessibleMenuIds.Contains(m.Id))
                    .ToListAsync();

                // Thêm parent menu nếu child menu được truy cập
                var parentMenuIds = accessibleMenus
                    .Where(m => m.ParentId.HasValue)
                    .Select(m => m.ParentId.Value)
                    .Distinct()
                    .ToList();

                var parentMenus = await _context.Menus
                    .Where(m => m.DeletedAt == null && parentMenuIds.Contains(m.Id))
                    .ToListAsync();

                // Merge tất cả menu
                var allMenus = accessibleMenus.Concat(parentMenus).Distinct().ToList();

                // Tạo cấu trúc hierarchical
                var rootMenus = allMenus
                    .Where(m => m.ParentId == null)
                    .OrderBy(m => m.DisplayOrder)
                    .Select(m => new
                    {
                        m.Id,
                        m.Name,
                        m.Path,
                        m.Icon,
                        m.DisplayOrder,
                        m.ParentId,
                        ChildMenus = BuildChildMenus(m, allMenus, accessibleMenuIds)
                    })
                    .ToList();

                return Ok(rootMenus);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi truy xuất dữ liệu: {ex.Message}");
            }
        }

        private List<object> BuildChildMenus(Menu parentMenu, List<Menu> allMenus, List<int> accessibleMenuIds)
        {
            return allMenus
                .Where(m => m.ParentId == parentMenu.Id && (accessibleMenuIds.Contains(m.Id) || HasAccessibleChildren(m, allMenus, accessibleMenuIds)))
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.Path,
                    m.Icon,
                    m.DisplayOrder,
                    m.ParentId,
                    ChildMenus = BuildChildMenus(m, allMenus, accessibleMenuIds)
                })
                .Cast<object>()
                .ToList();
        }

        private bool HasAccessibleChildren(Menu menu, List<Menu> allMenus, List<int> accessibleMenuIds)
        {
            var childMenus = allMenus.Where(m => m.ParentId == menu.Id);
            return childMenus.Any(child => accessibleMenuIds.Contains(child.Id) || HasAccessibleChildren(child, allMenus, accessibleMenuIds));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}