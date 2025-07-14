using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.PermissionMenu
{
    public class PermissionMenuUpdateDto
    {
        [Required]
        public int PermissionId { get; set; }
        [Required]
        public int MenuId { get; set; }
        // Thêm các trường khác nếu cần
    }
}
