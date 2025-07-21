using DataManagementApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DataManagementApi.Services
{
    /// <summary>
    /// Service để check matrix permissions cho user trong controllers
    /// </summary>
    public class MatrixPermissionService
    {
        private readonly ApplicationDbContext _context;

        public MatrixPermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Kiểm tra user có quyền specific permission cho một module không
        /// </summary>
        /// <param name="userClaims">User claims từ JWT</param>
        /// <param name="moduleName">Tên module (ví dụ: "User", "Thesis")</param>
        /// <param name="permissionType">Loại quyền ("Create", "Read", "Update", "Delete")</param>
        /// <returns>True nếu có quyền</returns>
        public async Task<bool> HasPermissionAsync(ClaimsPrincipal userClaims, string moduleName, string permissionType)
        {
            var keycloakUserId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return false;
            }

            var user = await _context.Users
                .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RoleModulePermissions)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return false;
            }

            // Lấy tất cả matrix permissions của user cho module cụ thể
            var modulePermissions = user.UserRoles
                .SelectMany(ur => ur.Role.RoleModulePermissions)
                .Where(rmp => rmp.ModuleName == moduleName && rmp.DeletedAt == null)
                .ToList();

            if (!modulePermissions.Any())
            {
                return false;
            }

            // Check permission type
            return permissionType.ToLower() switch
            {
                "create" => modulePermissions.Any(mp => mp.CanCreate),
                "read" => modulePermissions.Any(mp => mp.CanRead),
                "update" => modulePermissions.Any(mp => mp.CanUpdate),
                "delete" => modulePermissions.Any(mp => mp.CanDelete),
                _ => false
            };
        }

        /// <summary>
        /// Kiểm tra user có quyền CREATE cho module
        /// </summary>
        public async Task<bool> CanCreateAsync(ClaimsPrincipal userClaims, string moduleName)
        {
            return await HasPermissionAsync(userClaims, moduleName, "create");
        }

        /// <summary>
        /// Kiểm tra user có quyền READ cho module
        /// </summary>
        public async Task<bool> CanReadAsync(ClaimsPrincipal userClaims, string moduleName)
        {
            return await HasPermissionAsync(userClaims, moduleName, "read");
        }

        /// <summary>
        /// Kiểm tra user có quyền UPDATE cho module
        /// </summary>
        public async Task<bool> CanUpdateAsync(ClaimsPrincipal userClaims, string moduleName)
        {
            return await HasPermissionAsync(userClaims, moduleName, "update");
        }

        /// <summary>
        /// Kiểm tra user có quyền DELETE cho module
        /// </summary>
        public async Task<bool> CanDeleteAsync(ClaimsPrincipal userClaims, string moduleName)
        {
            return await HasPermissionAsync(userClaims, moduleName, "delete");
        }

        /// <summary>
        /// Lấy tất cả modules mà user có quyền READ
        /// </summary>
        public async Task<List<string>> GetReadableModulesAsync(ClaimsPrincipal userClaims)
        {
            var keycloakUserId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return new List<string>();
            }

            var user = await _context.Users
                .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RoleModulePermissions)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new List<string>();
            }

            return user.UserRoles
                .SelectMany(ur => ur.Role.RoleModulePermissions)
                .Where(rmp => rmp.CanRead && rmp.DeletedAt == null)
                .Select(rmp => rmp.ModuleName)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Lấy tất cả permissions của user cho một module cụ thể
        /// </summary>
        public async Task<ModulePermissionResult> GetModulePermissionsAsync(ClaimsPrincipal userClaims, string moduleName)
        {
            var keycloakUserId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return new ModulePermissionResult();
            }

            var user = await _context.Users
                .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RoleModulePermissions)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return new ModulePermissionResult();
            }

            var modulePermissions = user.UserRoles
                .SelectMany(ur => ur.Role.RoleModulePermissions)
                .Where(rmp => rmp.ModuleName == moduleName && rmp.DeletedAt == null)
                .ToList();

            return new ModulePermissionResult
            {
                ModuleName = moduleName,
                CanCreate = modulePermissions.Any(mp => mp.CanCreate),
                CanRead = modulePermissions.Any(mp => mp.CanRead),
                CanUpdate = modulePermissions.Any(mp => mp.CanUpdate),
                CanDelete = modulePermissions.Any(mp => mp.CanDelete)
            };
        }

        /// <summary>
        /// Kiểm tra user có phải ADMIN không (có full permissions cho tất cả modules)
        /// </summary>
        public async Task<bool> IsAdminAsync(ClaimsPrincipal userClaims)
        {
            var keycloakUserId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return false;
            }

            var user = await _context.Users
                .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return false;
            }

            return user.UserRoles.Any(ur => ur.Role.Name == "Admin");
        }
    }

    /// <summary>
    /// Result class cho module permissions
    /// </summary>
    public class ModulePermissionResult
    {
        public string ModuleName { get; set; } = string.Empty;
        public bool CanCreate { get; set; }
        public bool CanRead { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }
} 