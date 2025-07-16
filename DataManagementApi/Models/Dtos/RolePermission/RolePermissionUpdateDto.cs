using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.RolePermission
{
    public class RolePermissionUpdateDto
    {
        [Required]
        public int RoleId { get; set; }
        [Required]
        public int PermissionId { get; set; }
        public string? RoleName { get; set; }
        public string? PermissionName { get; set; }
        // Thêm các trường khác nếu cần
    }
}
