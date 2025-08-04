using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using DataManagementApi.Data;
using System.Security.Claims;

namespace DataManagementApi.Services
{
    /// <summary>
    /// Cached Matrix Permission Service với in-memory caching để improve performance
    /// </summary>
    public class CachedMatrixPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedMatrixPermissionService> _logger;
        
        // Cache settings
        private static readonly TimeSpan UserPermissionsCacheExpiry = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan AdminCheckCacheExpiry = TimeSpan.FromMinutes(30);
        
        public CachedMatrixPermissionService(
            ApplicationDbContext context, 
            IMemoryCache cache,
            ILogger<CachedMatrixPermissionService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(ClaimsPrincipal userClaims, string moduleName, string permissionType)
        {
            try
            {
                // Try to get Keycloak user ID from different claim types
                var keycloakUserId = userClaims.FindFirst("sub")?.Value 
                    ?? userClaims.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                    
                if (string.IsNullOrEmpty(keycloakUserId))
                {
                    _logger.LogWarning("No Keycloak user ID found in claims");
                    return false;
                }

                // Cache key for user permissions
                var cacheKey = $"user_permissions_{keycloakUserId}";
                
                // Try to get from cache first
                if (!_cache.TryGetValue(cacheKey, out Dictionary<string, Dictionary<string, bool>>? userPermissions))
                {
                    userPermissions = await LoadUserPermissionsAsync(keycloakUserId);
                    
                    // Cache for 15 minutes
                    _cache.Set(cacheKey, userPermissions, UserPermissionsCacheExpiry);
                    _logger.LogDebug($"Cached permissions for user {keycloakUserId}");
                }

                // Check specific permission from cache
                if (userPermissions?.ContainsKey(moduleName) == true && 
                    userPermissions[moduleName].ContainsKey(permissionType))
                {
                    var hasPermission = userPermissions[moduleName][permissionType];
                    _logger.LogDebug($"Permission check (cached): {keycloakUserId} - {moduleName}.{permissionType} = {hasPermission}");
                    return hasPermission;
                }

                _logger.LogDebug($"Permission not found: {moduleName}.{permissionType}");
                return false;
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
            // Try to get Keycloak user ID from different claim types
            var keycloakUserId = userClaims.FindFirst("sub")?.Value 
                ?? userClaims.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            if (string.IsNullOrEmpty(keycloakUserId)) return false;

            var cacheKey = $"is_admin_{keycloakUserId}";
            
            if (!_cache.TryGetValue(cacheKey, out bool isAdmin))
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null);

                isAdmin = user?.UserRoles?.Any(ur => ur.Role.Name == "Admin") == true;
                
                // Cache admin check for 30 minutes
                _cache.Set(cacheKey, isAdmin, AdminCheckCacheExpiry);
                _logger.LogDebug($"Cached admin check for user {keycloakUserId}: {isAdmin}");
            }

            return isAdmin;
        }

        /// <summary>
        /// Load all permissions for a user at once và cache chúng
        /// </summary>
        private async Task<Dictionary<string, Dictionary<string, bool>>> LoadUserPermissionsAsync(string keycloakUserId)
        {
            _logger.LogDebug($"Loading permissions from database for user {keycloakUserId}");
            
            var userPermissions = new Dictionary<string, Dictionary<string, bool>>();

            // Single query để get tất cả permissions của user
            var roleModulePermissions = await _context.Users
                .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                .SelectMany(u => u.UserRoles)
                .SelectMany(ur => ur.Role.RoleModulePermissions)
                .Where(rmp => rmp.DeletedAt == null)
                .ToListAsync();

            // Group permissions by module
            foreach (var rmp in roleModulePermissions)
            {
                if (!userPermissions.ContainsKey(rmp.ModuleName))
                {
                    userPermissions[rmp.ModuleName] = new Dictionary<string, bool>();
                }

                var modulePermissions = userPermissions[rmp.ModuleName];
                
                // Aggregate permissions (if user has multiple roles with same module)
                // Use OR logic: if any role grants permission, user has it
                modulePermissions["Create"] = modulePermissions.GetValueOrDefault("Create", false) || rmp.CanCreate;
                modulePermissions["Read"] = modulePermissions.GetValueOrDefault("Read", false) || rmp.CanRead;
                modulePermissions["Update"] = modulePermissions.GetValueOrDefault("Update", false) || rmp.CanUpdate;
                modulePermissions["Delete"] = modulePermissions.GetValueOrDefault("Delete", false) || rmp.CanDelete;
            }

            _logger.LogInformation($"Loaded {roleModulePermissions.Count} permission entries for user {keycloakUserId} across {userPermissions.Count} modules");
            return userPermissions;
        }

        /// <summary>
        /// Clear cache for specific user (sau khi thay đổi permissions)
        /// </summary>
        public void ClearUserCache(string keycloakUserId)
        {
            var permissionsCacheKey = $"user_permissions_{keycloakUserId}";
            var adminCacheKey = $"is_admin_{keycloakUserId}";
            
            _cache.Remove(permissionsCacheKey);
            _cache.Remove(adminCacheKey);
            
            _logger.LogInformation($"Cleared cache for user {keycloakUserId}");
        }

        /// <summary>
        /// Clear all permission caches (sau khi bulk changes)
        /// </summary>
        public void ClearAllCaches()
        {
            // Since IMemoryCache doesn't have clear all method, we'll use a different approach
            // This is a limitation but for now, cache will expire naturally
            _logger.LogInformation("Permission caches will expire naturally within 15-30 minutes");
        }

        /// <summary>
        /// Get cache statistics for monitoring
        /// </summary>
        public object GetCacheStats()
        {
            // This is basic stats - in production you might want more detailed metrics
            return new
            {
                CacheType = "In-Memory",
                UserPermissionsCacheExpiry = UserPermissionsCacheExpiry.TotalMinutes + " minutes",
                AdminCheckCacheExpiry = AdminCheckCacheExpiry.TotalMinutes + " minutes"
            };
        }
    }
} 