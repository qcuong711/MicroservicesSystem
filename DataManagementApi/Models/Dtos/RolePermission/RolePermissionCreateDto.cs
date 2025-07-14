using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.RolePermission
{
    public class RolePermissionCreateDto
    {
        [Required]
        public int RoleId { get; set; }
        [Required]
        public int PermissionId { get; set; }
        // Thêm các trường khác nếu cần
    }
}
