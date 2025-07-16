using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.UserPermission
{
    public class UserPermissionUpdateDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int PermissionId { get; set; }
        // Thêm các trường khác nếu cần
    }
}
