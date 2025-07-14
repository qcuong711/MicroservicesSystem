using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataManagementApi.Models
{
    [Table("InternshipPeriods")]
    public class InternshipPeriod
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        // Foreign key to AcademicYear
        [Required]
        [ForeignKey("AcademicYear")]
        public int AcademicYearId { get; set; }
        public AcademicYear? AcademicYear { get; set; }
        // Foreign key to Semester
        [Required]
        [ForeignKey("Semester")]
        public int SemesterId { get; set; }
        public Semester? Semester { get; set; }
        public ICollection<Internship>? Internships { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
