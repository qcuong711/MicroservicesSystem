using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Permission
{
    public class PermissionUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string? Description { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Permission
        public string? Id { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}
