using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DataManagementApi.Models.Dtos.Thesis
{
    public class StudentThesisSubmitDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public int ThesisPeriodId { get; set; }
        
        [Required]
        public int AcademicYearId { get; set; }
        
        [Required]
        public int SemesterId { get; set; }
        
        // File upload property
        public IFormFile? ThesisFile { get; set; }
    }
}