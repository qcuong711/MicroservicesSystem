using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Permission
{
    public class PermissionCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string? Description { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Permission
    }
}
