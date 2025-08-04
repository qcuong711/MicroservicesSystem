using Microsoft.EntityFrameworkCore;
using DataManagementApi.Data;
using DataManagementApi.Extensions;
using System.Security.Claims;

namespace DataManagementApi.Services
{
    /// <summary>
    /// Simple Matrix Permission Service - Không cache, truy vấn database trực tiếp
    /// Dùng để test và debug
    /// </summary>
    public class SimpleMatrixPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SimpleMatrixPermissionService> _logger;
        
        public SimpleMatrixPermissionService(
            ApplicationDbContext context,
            ILogger<SimpleMatrixPermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal userClaims, string moduleName, string permissionType)
        {
            try
            {
                // Lấy Keycloak User ID từ claims sử dụng extension method
                var keycloakUserId = userClaims.GetKeycloakUserId();
                if (string.IsNullOrEmpty(keycloakUserId))
                {
                    _logger.LogWarning("No Keycloak user ID found in claims");
                    return false;
                }

                _logger.LogDebug($"Checking permission: {keycloakUserId} - {moduleName}.{permissionType}");

                // Truy vấn database trực tiếp - không cache
                var hasPermission = await _context.Users
                    .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                    .SelectMany(u => u.UserRoles)
                    .Where(ur => ur.Role.DeletedAt == null)
                    .SelectMany(ur => ur.Role.RoleModulePermissions)
                    .Where(rmp => rmp.ModuleName == moduleName && rmp.DeletedAt == null)
                    .AnyAsync(rmp => 
                        (permissionType == "Create" && rmp.CanCreate) ||
                        (permissionType == "Read" && rmp.CanRead) ||
                        (permissionType == "Update" && rmp.CanUpdate) ||
                        (permissionType == "Delete" && rmp.CanDelete)
                    );

                _logger.LogInformation($"Permission check (direct): {keycloakUserId} - {moduleName}.{permissionType} = {hasPermission}");
                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking permission {moduleName}.{permissionType}");
                return false;
            }
        }

        public async Task<bool> CanCreateAsync(ClaimsPrincipal userClaims, string moduleName)
        {
            return await HasPermissionAsync(userClaims, moduleName, "Create");
        }

        public async Task<bool> CanReadAsync(ClaimsPrincipal userClaims, string moduleName)
        {
            return await HasPermissionAsync(userClaims, moduleName, "Read");
        }

        public async Task<bool> CanUpdateAsync(ClaimsPrincipal userClaims, string moduleName)
        {
            return await HasPermissionAsync(userClaims, moduleName, "Update");
        }

        public async Task<bool> CanDeleteAsync(ClaimsPrincipal userClaims, string moduleName)
        {
            return await HasPermissionAsync(userClaims, moduleName, "Delete");
        }

        public async Task<bool> IsAdminAsync(ClaimsPrincipal userClaims)
        {
            try
            {
                var keycloakUserId = userClaims.GetKeycloakUserId();
                if (string.IsNullOrEmpty(keycloakUserId)) return false;

                _logger.LogDebug($"Checking admin status for: {keycloakUserId}");

                // Truy vấn database trực tiếp - không cache
                var isAdmin = await _context.Users
                    .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                    .SelectMany(u => u.UserRoles)
                    .Where(ur => ur.Role.DeletedAt == null)
                    .AnyAsync(ur => ur.Role.Name.ToLower() == "admin" || ur.Role.Name.ToLower() == "administrator");

                _logger.LogInformation($"Admin check (direct): {keycloakUserId} = {isAdmin}");
                return isAdmin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking admin status");
                return false;
            }
        }

        // Removed GetKeycloakUserId method - now using extension method

        /// <summary>
        /// Load toàn bộ permissions của user (dùng cho debug)
        /// </summary>
        public async Task<Dictionary<string, Dictionary<string, bool>>> GetUserPermissionsAsync(ClaimsPrincipal userClaims)
        {
            var keycloakUserId = userClaims.GetKeycloakUserId();
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return new Dictionary<string, Dictionary<string, bool>>();
            }

            var userPermissions = new Dictionary<string, Dictionary<string, bool>>();

            var roleModulePermissions = await _context.Users
                .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RoleModulePermissions)
                .Where(rmp => rmp.DeletedAt == null)
                .ToListAsync();

            foreach (var rmp in roleModulePermissions)
            {
                if (!userPermissions.ContainsKey(rmp.ModuleName))
                {
                    userPermissions[rmp.ModuleName] = new Dictionary<string, bool>();
                }

                var modulePermissions = userPermissions[rmp.ModuleName];
                
                // OR logic: nếu có role nào grant permission thì user có permission đó
                modulePermissions["Create"] = modulePermissions.GetValueOrDefault("Create", false) || rmp.CanCreate;
                modulePermissions["Read"] = modulePermissions.GetValueOrDefault("Read", false) || rmp.CanRead;
                modulePermissions["Update"] = modulePermissions.GetValueOrDefault("Update", false) || rmp.CanUpdate;
                modulePermissions["Delete"] = modulePermissions.GetValueOrDefault("Delete", false) || rmp.CanDelete;
            }

            _logger.LogInformation($"Loaded permissions for user {keycloakUserId}: {userPermissions.Count} modules");
            return userPermissions;
        }
    }
} 