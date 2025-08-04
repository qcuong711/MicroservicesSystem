using Microsoft.AspNetCore.Authorization;
using DataManagementApi.Services;
using System.Text.RegularExpressions;

namespace DataManagementApi.Authorization
{
    /// <summary>
    /// Global Authorization Handler tự động check Matrix Permissions cho tất cả endpoints
    /// </summary>
    public class GlobalMatrixAuthorizationHandler : AuthorizationHandler<GlobalMatrixRequirement>
    {
        private readonly SimpleMatrixPermissionService _simpleMatrixPermissionService;
        private readonly ILogger<GlobalMatrixAuthorizationHandler> _logger;
        private readonly PermissionAuditService _auditService;

        public GlobalMatrixAuthorizationHandler(
            SimpleMatrixPermissionService simpleMatrixPermissionService,
            ILogger<GlobalMatrixAuthorizationHandler> logger,
            PermissionAuditService auditService)
        {
            _simpleMatrixPermissionService = simpleMatrixPermissionService;
            _logger = logger;
            _auditService = auditService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            GlobalMatrixRequirement requirement)
        {
            // Skip nếu user chưa authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Fail();
                return;
            }

            try
            {
                var httpContext = context.Resource as Microsoft.AspNetCore.Http.DefaultHttpContext;
                if (httpContext == null)
                {
                    context.Fail();
                    return;
                }

                var path = httpContext.Request.Path.Value?.ToLower();
                var method = httpContext.Request.Method.ToUpper();

                // Skip các endpoints không cần check permissions
                if (ShouldSkipAuthorization(path, method))
                {
                    context.Succeed(requirement);
                    return;
                }

                // Extract module và permission type từ route
                var (moduleName, permissionType) = ExtractModuleAndPermission(path, method);

                if (string.IsNullOrEmpty(moduleName) || string.IsNullOrEmpty(permissionType))
                {
                    _logger.LogWarning($"Cannot determine module/permission for {method} {path}");
                    context.Succeed(requirement); // Allow by default nếu không xác định được
                    return;
                }

                // Check matrix permission (direct database query - no cache)
                var hasPermission = await _simpleMatrixPermissionService.HasPermissionAsync(
                    context.User, 
                    moduleName, 
                    permissionType
                );

                // Log audit trail (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var deniedReason = hasPermission ? null : $"User lacks {permissionType} permission for {moduleName} module";
                        await _auditService.LogPermissionCheckAsync(
                            context.User,
                            moduleName,
                            permissionType,
                            hasPermission,
                            httpContext,
                            resource: ExtractResourceId(httpContext.Request.Path.Value),
                            deniedReason: deniedReason
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log audit trail for permission check");
                    }
                });

                if (hasPermission)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning($"Access denied: User lacks {permissionType} permission for {moduleName} module");
                    context.Fail();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GlobalMatrixAuthorizationHandler");
                context.Fail();
            }
        }

        /// <summary>
        /// Kiểm tra các endpoints không cần check permissions
        /// </summary>
        private static bool ShouldSkipAuthorization(string? path, string method)
        {
            if (string.IsNullOrEmpty(path)) return true;

            // Skip các endpoints public
            var skipPaths = new[]
            {
                "/api/debug",           // Debug endpoints
                "/api/users/me",        // Get current user info
                "/api/users/me/menus",  // Get user menus
                "/api/cache",           // Cache management endpoints
                "/swagger",             // Swagger docs
                "/health",              // Health check
                "/api/selections",      // Selection dropdown APIs
                "/api/permissions/stream", // SSE stream endpoint
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath));
        }

        /// <summary>
        /// Extract module name và permission type từ route và HTTP method
        /// </summary>
        private static (string moduleName, string permissionType) ExtractModuleAndPermission(string? path, string method)
        {
            if (string.IsNullOrEmpty(path)) return ("", "");

            // Sử dụng ModuleRegistry để tìm module từ API path
            string moduleName = ModuleRegistry.GetModuleByApiPath(path) ?? "";

            if (string.IsNullOrEmpty(moduleName)) return ("", "");

            // Map HTTP method → Permission type
            var permissionType = method switch
            {
                "GET" => "Read",
                "POST" when path.Contains("restore") => "Update", // Restore = Update
                "POST" when path.Contains("delete") => "Delete", // Soft delete = Delete
                "POST" => "Create", // Regular POST = Create
                "PUT" => "Update",
                "PATCH" => "Update", 
                "DELETE" => "Delete",
                _ => "Read" // Default to Read
            };

            return (moduleName, permissionType);
        }

        /// <summary>
        /// Extract resource ID from request path for audit logging
        /// </summary>
        private static string? ExtractResourceId(string? path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Try to extract ID from common REST patterns
            // e.g., /api/users/123 -> "123"
            // e.g., /api/users/123/roles -> "123"
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // Look for numeric IDs after API endpoints
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i] == "api" && i + 2 < segments.Length)
                {
                    // Check if the segment after the module name is a number
                    if (int.TryParse(segments[i + 2], out var id))
                    {
                        return id.ToString();
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Requirement cho Global Matrix Authorization
    /// </summary>
    public class GlobalMatrixRequirement : IAuthorizationRequirement
    {
        // Empty requirement class
    }
} 