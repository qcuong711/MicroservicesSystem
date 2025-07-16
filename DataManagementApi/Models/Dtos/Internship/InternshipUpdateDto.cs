using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Internship
{
    public class InternshipUpdateDto
    {
        [StringLength(100)]
        public string? Title { get; set; }
        public int? StudentId { get; set; }
        public int? PartnerId { get; set; }
        public int? InternshipPeriodId { get; set; }
        public string? ReportUrl { get; set; }
        public double? Grade { get; set; }
    }
}
