using System.Security.Claims;

namespace DataManagementApi.Extensions
{
    /// <summary>
    /// Extension methods cho ClaimsPrincipal để lấy Keycloak User ID consistent
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// Lấy Keycloak User ID từ JWT claims với fallback logic
        /// </summary>
        public static string? GetKeycloakUserId(this ClaimsPrincipal claimsPrincipal)
        {
            // Thử claim standard "sub" trước
            var userId = claimsPrincipal.FindFirst("sub")?.Value;
            
            // Fallback to Microsoft claim format
            if (string.IsNullOrEmpty(userId))
            {
                userId = claimsPrincipal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            }
            
            // Additional fallback for other formats
            if (string.IsNullOrEmpty(userId))
            {
                userId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            
            return userId;
        }
        
        /// <summary>  
        /// Lấy username từ JWT claims
        /// </summary>
        public static string? GetUsername(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirst("preferred_username")?.Value
                ?? claimsPrincipal.FindFirst("username")?.Value
                ?? claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value;
        }
        
        /// <summary>
        /// Lấy email từ JWT claims
        /// </summary>
        public static string? GetEmail(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value
                ?? claimsPrincipal.FindFirst("email")?.Value
                ?? claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
        }
    }
} 