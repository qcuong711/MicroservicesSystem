using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Menu
{
    public class MenuUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public int? ParentId { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Menu
    }
}
