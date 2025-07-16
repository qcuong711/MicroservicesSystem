using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Department
{
    public class DepartmentUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        public int? ParentDepartmentId { get; set; }
    }
}
