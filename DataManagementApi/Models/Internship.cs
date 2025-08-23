using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataManagementApi.Models
{
    [Table("Internships")]
    public class Internship
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Foreign key to User (not Student)
        [Required]
        [ForeignKey("Student")]
        public int StudentId { get; set; }
        
        [ForeignKey("StudentId")]
        public User? Student { get; set; }

        // Foreign key to Partner
        [Required]
        [ForeignKey("Partner")]
        public int PartnerId { get; set; }
        
        [ForeignKey("PartnerId")]
        public Partner? Partner { get; set; }

        // Foreign key to AcademicYear
        [Required]
        [ForeignKey("AcademicYear")]
        public int AcademicYearId { get; set; }
        
        [ForeignKey("AcademicYearId")]
        public AcademicYear? AcademicYear { get; set; }

        // Foreign key to Semester
        [Required]
        [ForeignKey("Semester")]
        public int SemesterId { get; set; }
        
        [ForeignKey("SemesterId")]
        public Semester? Semester { get; set; }

        // Foreign key to InternshipPeriod
        [Required]
        [ForeignKey("InternshipPeriod")]
        public int InternshipPeriodId { get; set; }
        public InternshipPeriod? InternshipPeriod { get; set; }

        public string? ReportUrl { get; set; }
        
        public double? Grade { get; set; }

        public string? Status { get; set; } // Draft, Submitted, Approved, Rejected
        
        public DateTime? DeletedAt { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
    }
}