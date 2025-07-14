using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Student
{
    public class StudentUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string StudentCode { get; set; } = string.Empty;

        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Student
    }
}
