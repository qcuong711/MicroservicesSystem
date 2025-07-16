using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.RoleMenu
{
    public class RoleMenuUpdateDto
    {
        [Required]
        public int RoleId { get; set; }
        [Required]
        public int MenuId { get; set; }
        // Thêm các trường khác nếu cần
    }
}
