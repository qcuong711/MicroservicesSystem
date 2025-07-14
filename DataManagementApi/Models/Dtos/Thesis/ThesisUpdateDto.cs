using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Thesis
{
    public class ThesisUpdateDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        public int StudentId { get; set; }
        [Required]
        public int SupervisorId { get; set; }
        public int? ExaminerId { get; set; }
        [Required]
        public int ThesisPeriodId { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? Status { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Thesis
    }
}
