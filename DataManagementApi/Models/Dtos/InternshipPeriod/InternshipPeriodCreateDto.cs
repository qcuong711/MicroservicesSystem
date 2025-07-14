using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.InternshipPeriod
{
    public class InternshipPeriodCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AcademicYearId { get; set; }
        public int SemesterId { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model InternshipPeriod
    }
}
