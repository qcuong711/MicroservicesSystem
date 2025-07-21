using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DataManagementApi.Services;
using System.Security.Claims;

namespace DataManagementApi.Attributes
{
    /// <summary>
    /// Custom Authorization Attribute để check Matrix Permissions
    /// </summary>
    public class MatrixPermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _moduleName;
        private readonly string _permissionType;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="moduleName">Module name (ví dụ: "Thesis", "User")</param>
        /// <param name="permissionType">Permission type ("Create", "Read", "Update", "Delete")</param>
        public MatrixPermissionAttribute(string moduleName, string permissionType)
        {
            _moduleName = moduleName;
            _permissionType = permissionType;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Check xem user đã authenticated chưa
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedObjectResult(new { 
                    message = "User chưa được xác thực" 
                });
                return;
            }

            // Get MatrixPermissionService từ DI container
            var permissionService = context.HttpContext.RequestServices
                .GetService<MatrixPermissionService>();

            if (permissionService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            try
            {
                // Check matrix permission
                var hasPermission = await permissionService.HasPermissionAsync(
                    context.HttpContext.User, 
                    _moduleName, 
                    _permissionType
                );

                if (!hasPermission)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }
            catch (Exception ex)
            {
                // Log error và trả về 403
                Console.WriteLine($"MatrixPermissionAttribute Error: {ex.Message}");
                context.Result = new ForbidResult();
                return;
            }
        }
    }

    /// <summary>
    /// Convenience attributes cho từng loại permission
    /// </summary>
    public class MatrixCreateAttribute : MatrixPermissionAttribute
    {
        public MatrixCreateAttribute(string moduleName) : base(moduleName, "Create") { }
    }

    public class MatrixReadAttribute : MatrixPermissionAttribute
    {
        public MatrixReadAttribute(string moduleName) : base(moduleName, "Read") { }
    }

    public class MatrixUpdateAttribute : MatrixPermissionAttribute
    {
        public MatrixUpdateAttribute(string moduleName) : base(moduleName, "Update") { }
    }

    public class MatrixDeleteAttribute : MatrixPermissionAttribute
    {
        public MatrixDeleteAttribute(string moduleName) : base(moduleName, "Delete") { }
    }
} 