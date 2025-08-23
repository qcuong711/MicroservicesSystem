using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DataManagementApi.Models.Dtos.Thesis
{
    public class ThesisCreateDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int SupervisorId { get; set; }
        
        public List<int>? SupervisorIds { get; set; } // Danh sách giảng viên hướng dẫn
        
        public int? ExaminerId { get; set; }
        
        public List<int>? ExaminerIds { get; set; } // Danh sách giảng viên phản biện (không bắt buộc)
        
        [Required]
        public int ThesisPeriodId { get; set; }
        
        [Required]
        public int AcademicYearId { get; set; }
        
        [Required]
        public int SemesterId { get; set; }
        
        public DateTime SubmissionDate { get; set; }
        
        public string? Status { get; set; } // Draft, Submitted, Approved, Rejected
        
        // File upload property
        public IFormFile? ThesisFile { get; set; }
        
        // Score property (chỉ giảng viên mới có thể cập nhật)
        public decimal? Score { get; set; }
    }
}

