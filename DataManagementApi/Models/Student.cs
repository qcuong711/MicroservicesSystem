using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataManagementApi.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(1000)]
        public string? Note { get; set; }
        
        public int? DepartmentId { get; set; }
        
        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }
        
        public int? CourseClassId { get; set; }
        
        [ForeignKey("CourseClassId")]
        public CourseClass? CourseClass { get; set; }
        
        public DateTime? DeletedAt { get; set; }
    }
}