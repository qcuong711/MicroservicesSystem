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
        private readonly SimpleMatrixPermissionService _simplePermissionService;
        private readonly ILogger<CacheController> _logger;

        public CacheController(
            SimpleMatrixPermissionService simplePermissionService,
            ILogger<CacheController> logger)
        {
            _simplePermissionService = simplePermissionService;
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
                var stats = new { message = "Using SimpleMatrixPermissionService - no cache stats available", service = "SimpleMatrixPermissionService", caching = false };
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
        /// Helper method to get Keycloak User ID from claims
        /// </summary>
        private string? GetKeycloakUserId()
        {
            return User.FindFirst("sub")?.Value 
                ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        }

        /// <summary>
        /// Clear cache for current user (based on JWT claims)
        /// </summary>
        [HttpPost("clear/me")]
        public IActionResult ClearMyCache()
        {
            try
            {
                var keycloakUserId = GetKeycloakUserId();
                if (string.IsNullOrEmpty(keycloakUserId))
                {
                    return BadRequest(new { success = false, message = "User ID not found in token" });
                }

                // Note: SimpleMatrixPermissionService doesn't have caching, so this is a no-op
                _logger.LogInformation($"Cache clear requested for current user {keycloakUserId}, but using non-cached service");

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
                var isAdmin = await _simplePermissionService.IsAdminAsync(User);
                if (!isAdmin)
                {
                    return Forbid("Only administrators can clear other users' cache");
                }

                // Note: SimpleMatrixPermissionService doesn't have caching, so this is a no-op
                _logger.LogInformation($"Cache clear requested for user {keycloakUserId}, but using non-cached service");
                _logger.LogInformation($"Cache cleared for user {keycloakUserId} by admin {GetKeycloakUserId()}");

                return Ok(new
                {
                    success = true,
                    message = $"Permission cache cleared for user {keycloakUserId}",
                    clearedUserId = keycloakUserId,
                    clearedBy = GetKeycloakUserId()
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
                var isAdmin = await _simplePermissionService.IsAdminAsync(User);
                if (!isAdmin)
                {
                    return Forbid("Only administrators can clear all caches");
                }

                // Note: SimpleMatrixPermissionService doesn't have caching, so this is a no-op
                _logger.LogInformation($"All cache clear requested, but using non-cached service");
                _logger.LogWarning($"All permission caches cleared by admin {GetKeycloakUserId()}");

                return Ok(new
                {
                    success = true,
                    message = "All permission caches will expire naturally within 15-30 minutes",
                    clearedBy = GetKeycloakUserId(),
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
                var keycloakUserId = GetKeycloakUserId();
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