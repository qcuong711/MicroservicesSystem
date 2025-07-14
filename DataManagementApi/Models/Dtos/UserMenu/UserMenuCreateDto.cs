using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.UserMenu
{
    public class UserMenuCreateDto
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int MenuId { get; set; }
        // Thêm các trường khác nếu cần
    }
}
