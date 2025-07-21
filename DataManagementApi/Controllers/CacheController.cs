using DataManagementApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataManagementApi.Controllers
{
    [Route("api/cache")]
    [ApiController]
    [Authorize] // Require authentication
    public class CacheController : ControllerBase
    {
        private readonly CachedMatrixPermissionService _cachedPermissionService;
        private readonly ILogger<CacheController> _logger;

        public CacheController(
            CachedMatrixPermissionService cachedPermissionService,
            ILogger<CacheController> logger)
        {
            _cachedPermissionService = cachedPermissionService;
            _logger = logger;
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetCacheStats()
        {
            try
            {
                var stats = _cachedPermissionService.GetCacheStats();
                return Ok(new
                {
                    success = true,
                    data = stats,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache stats");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Clear cache for current user (based on JWT claims)
        /// </summary>
        [HttpPost("clear/me")]
        public IActionResult ClearMyCache()
        {
            try
            {
                var keycloakUserId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(keycloakUserId))
                {
                    return BadRequest(new { success = false, message = "User ID not found in token" });
                }

                _cachedPermissionService.ClearUserCache(keycloakUserId);
                _logger.LogInformation($"Cache cleared for user {keycloakUserId}");

                return Ok(new
                {
                    success = true,
                    message = "Your permission cache has been cleared",
                    userId = keycloakUserId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing user cache");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Clear cache for specific user (Admin only)
        /// </summary>
        [HttpPost("clear/user/{keycloakUserId}")]
        public async Task<IActionResult> ClearUserCache(string keycloakUserId)
        {
            try
            {
                // Check if current user is admin
                var isAdmin = await _cachedPermissionService.IsAdminAsync(User);
                if (!isAdmin)
                {
                    return Forbid("Only administrators can clear other users' cache");
                }

                _cachedPermissionService.ClearUserCache(keycloakUserId);
                _logger.LogInformation($"Cache cleared for user {keycloakUserId} by admin {User.FindFirst("sub")?.Value}");

                return Ok(new
                {
                    success = true,
                    message = $"Permission cache cleared for user {keycloakUserId}",
                    clearedUserId = keycloakUserId,
                    clearedBy = User.FindFirst("sub")?.Value
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing user cache");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Clear all permission caches (Admin only)
        /// </summary>
        [HttpPost("clear/all")]
        public async Task<IActionResult> ClearAllCaches()
        {
            try
            {
                // Check if current user is admin
                var isAdmin = await _cachedPermissionService.IsAdminAsync(User);
                if (!isAdmin)
                {
                    return Forbid("Only administrators can clear all caches");
                }

                _cachedPermissionService.ClearAllCaches();
                _logger.LogWarning($"All permission caches cleared by admin {User.FindFirst("sub")?.Value}");

                return Ok(new
                {
                    success = true,
                    message = "All permission caches will expire naturally within 15-30 minutes",
                    clearedBy = User.FindFirst("sub")?.Value,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all caches");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Health check endpoint for cache system
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            try
            {
                // Simple health check
                var keycloakUserId = User.FindFirst("sub")?.Value;
                var health = new
                {
                    status = "healthy",
                    cacheSystem = "in-memory",
                    userAuthenticated = !string.IsNullOrEmpty(keycloakUserId),
                    timestamp = DateTime.UtcNow
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache health check failed");
                return StatusCode(500, new
                {
                    status = "unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
} 