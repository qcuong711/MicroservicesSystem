using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Business
{
    public class BusinessCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }
}
