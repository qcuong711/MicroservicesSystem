using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.ThesisPeriod
{
    public class ThesisPeriodCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        [Required]
        public int AcademicYearId { get; set; }
        // Thêm các trường khác nếu cần
        [Required]
        public int SemesterId { get; set; } = 3; // Mặc định là 3 nếu không có SemesterId (học kì hè)
    }
}
