using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.UserRole
{
    public class UserRoleCreateDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int RoleId { get; set; }
        // Thêm các trường khác nếu cần
    }
}
