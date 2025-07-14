using System.Collections.Generic;

namespace DataManagementApi.Models.Dto
{
    public class CreateUserDto
    {
        public string KeycloakUserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Danh sách các Role ID cần gán cho user khi tạo mới
        public List<int>? RoleIds { get; set; } = new List<int>();
    }
} 