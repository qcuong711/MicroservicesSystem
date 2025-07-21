using DataManagementApi.Data;
using DataManagementApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/debug")]
    [ApiController]
    public class DebugController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly AdminUserSeeder _adminSeeder;
    private readonly CachedMatrixPermissionService _cachedPermissionService;

    public DebugController(
        ApplicationDbContext context, 
        AdminUserSeeder adminSeeder,
        CachedMatrixPermissionService cachedPermissionService)
    {
        _context = context;
        _adminSeeder = adminSeeder;
        _cachedPermissionService = cachedPermissionService;
    }

    // GET: api/debug/users
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsersWithRoles()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Where(u => u.DeletedAt == null)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.KeycloakUserId,
                u.Name,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
            })
            .ToListAsync();

        return Ok(users);
    }

    // GET: api/debug/matrix-permissions
    [HttpGet("matrix-permissions")]
    public async Task<IActionResult> GetMatrixPermissions()
    {
        var permissions = await _context.RoleModulePermissions
            .Include(rmp => rmp.Role)
            .Select(rmp => new
            {
                rmp.Id,
                RoleName = rmp.Role.Name,
                rmp.ModuleName,
                rmp.CanCreate,
                rmp.CanRead,
                rmp.CanUpdate,
                rmp.CanDelete
            })
            .ToListAsync();

        return Ok(permissions);
    }

    // GET: api/debug/menus
    [HttpGet("menus")]
    public async Task<IActionResult> GetAllMenus()
    {
        var menus = await _context.Menus
            .Where(m => m.DeletedAt == null)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new
            {
                m.Id,
                m.Name,
                m.Path,
                m.Icon,
                m.DisplayOrder,
                m.ParentId
            })
            .ToListAsync();

        return Ok(menus);
    }

    // POST: api/debug/promote-user/{emailOrKeycloakId}
    [HttpPost("promote-user/{emailOrKeycloakId}")]
    public async Task<IActionResult> PromoteUserToAdmin(string emailOrKeycloakId)
    {
        await _adminSeeder.PromoteUserToAdminAsync(emailOrKeycloakId);
        return Ok(new { message = $"Attempted to promote user: {emailOrKeycloakId}" });
    }

    // POST: api/debug/create-admin
    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateDefaultAdmin()
    {
        await _adminSeeder.SeedDefaultAdminAsync();
        return Ok(new { message = "Attempted to create default admin user" });
    }

    // GET: api/debug/test-matrix/{userEmail}
    [HttpGet("test-matrix/{userEmail}")]
    public async Task<IActionResult> TestMatrixForUser(string userEmail)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RoleModulePermissions)
            .FirstOrDefaultAsync(u => u.Email == userEmail);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var userModulesWithReadAccess = user.UserRoles
            .SelectMany(ur => ur.Role.RoleModulePermissions)
            .Where(rmp => rmp.CanRead && rmp.DeletedAt == null)
            .Select(rmp => rmp.ModuleName)
            .Distinct()
            .ToList();

        var accessibleMenuPaths = Services.ModuleMenuMappingService.GetAccessibleMenuPaths(userModulesWithReadAccess);

        return Ok(new
        {
            UserEmail = user.Email,
            UserRoles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            ModulesWithReadAccess = userModulesWithReadAccess,
            AccessibleMenuPaths = accessibleMenuPaths
        });
    }

    // GET: api/debug/cache-performance
    [HttpGet("cache-performance")]
    public IActionResult GetCachePerformanceStats()
    {
        return Ok(new
        {
            CacheStats = _cachedPermissionService.GetCacheStats(),
            SystemInfo = new
            {
                TotalUsers = _context.Users.Count(u => u.DeletedAt == null),
                TotalRoles = _context.Roles.Count(r => r.DeletedAt == null),
                TotalMatrixPermissions = _context.RoleModulePermissions.Count(rmp => rmp.DeletedAt == null),
                Timestamp = DateTime.UtcNow
            },
            Performance = new
            {
                Message = "Cache hits reduce DB queries from ~3-5 per request to 0 (cached) or 1 (cache miss)",
                CacheExpiry = "15 minutes for permissions, 30 minutes for admin checks",
                RecommendedAction = "Clear cache after role/permission changes"
            }
        });
    }
    }
} 