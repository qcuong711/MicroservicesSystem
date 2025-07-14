using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.InternshipPeriod
{
    public class InternshipPeriodUpdateDto
    {
        [StringLength(100)]
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? AcademicYearId { get; set; }
        public int? SemesterId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
