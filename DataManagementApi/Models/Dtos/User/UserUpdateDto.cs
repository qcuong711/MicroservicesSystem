using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.User
{
    public class UserUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string KeycloakUserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public List<int>? RoleIds { get; set; } = new List<int>();
        // Giữ các trường cũ, bổ sung đầy đủ field theo model User
    }
}
