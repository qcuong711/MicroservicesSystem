using System.Collections.Generic;

namespace DataManagementApi.Models.Dtos.User
{
    public class CreateUserDto
    {
        public string KeycloakUserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int>? RoleIds { get; set; } = new List<int>();
    }
}