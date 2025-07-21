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
        private readonly CachedMatrixPermissionService _cachedMatrixPermissionService;
        private readonly ILogger<GlobalMatrixAuthorizationHandler> _logger;

        public GlobalMatrixAuthorizationHandler(
            CachedMatrixPermissionService cachedMatrixPermissionService,
            ILogger<GlobalMatrixAuthorizationHandler> logger)
        {
            _cachedMatrixPermissionService = cachedMatrixPermissionService;
            _logger = logger;
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

                // Check matrix permission (with caching)
                var hasPermission = await _cachedMatrixPermissionService.HasPermissionAsync(
                    context.User, 
                    moduleName, 
                    permissionType
                );

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
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath));
        }

        /// <summary>
        /// Extract module name và permission type từ route và HTTP method
        /// </summary>
        private static (string moduleName, string permissionType) ExtractModuleAndPermission(string? path, string method)
        {
            if (string.IsNullOrEmpty(path)) return ("", "");

            // Route mapping: API path → Module name
            var moduleMapping = new Dictionary<string, string>
            {
                { "/api/users", "User" },
                { "/api/roles", "Role" },
                { "/api/permissions", "Role" }, // Permissions thuộc về Role module
                { "/api/students", "Student" },
                { "/api/lecturers", "Lecturer" },
                { "/api/departments", "Department" },
                { "/api/partners", "Partner" },
                { "/api/businesses", "Business" },
                { "/api/theses", "Thesis" },
                { "/api/thesis-periods", "ThesisPeriod" },
                { "/api/internships", "InternshipPeriod" },
                { "/api/internship-periods", "InternshipPeriod" },
                { "/api/academic-years", "AcademicYear" },
                { "/api/semesters", "Semester" },
                { "/api/menus", "Menu" },
                { "/api/system-settings", "Settings" },
                { "/api/role-module-permissions", "Role" } // Matrix permissions thuộc về Role
            };

            // Tìm module name từ path
            string moduleName = "";
            foreach (var mapping in moduleMapping)
            {
                if (path.StartsWith(mapping.Key))
                {
                    moduleName = mapping.Value;
                    break;
                }
            }

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
    }

    /// <summary>
    /// Requirement cho Global Matrix Authorization
    /// </summary>
    public class GlobalMatrixRequirement : IAuthorizationRequirement
    {
        // Empty requirement class
    }
} 