using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Role
{
    public class RoleUpdateDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Role
    }
}
