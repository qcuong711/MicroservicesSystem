using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Internship
{
    public class InternshipCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        [Required]
        public int StudentId { get; set; }
        [Required]
        public int PartnerId { get; set; }
        [Required]
        public int InternshipPeriodId { get; set; }
        public string? ReportUrl { get; set; }
        public double? Grade { get; set; }
        public string? Status { get; set; }
    }
}
