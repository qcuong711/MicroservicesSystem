using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Semester
{
    public class SemesterCreateDto
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        public int AcademicYearId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Semester
    }
}
