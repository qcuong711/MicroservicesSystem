using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.AcademicYear
{
    public class AcademicYearCreateDto
    {
        [Required]
        [StringLength(20)]
        public string Year { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string Name { get; set; } = string.Empty;
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model AcademicYear
    }
}
