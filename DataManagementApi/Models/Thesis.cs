namespace DataManagementApi.Models
{
    public class Thesis
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }

        // Foreign key to Student
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        // Foreign key to Supervisor (Giảng viên hướng dẫn)
        public int SupervisorId { get; set; }
        public Lecturer? Supervisor { get; set; }

        // Foreign key to Examiner (Giảng viên phản biện - optional)
        public int? ExaminerId { get; set; }
        public Lecturer? Examiner { get; set; }

        // Foreign key to ThesisPeriod
        public int ThesisPeriodId { get; set; }
        public ThesisPeriod? ThesisPeriod { get; set; }

        // Foreign key to AcademicYear
        public int AcademicYearId { get; set; }
        public AcademicYear? AcademicYear { get; set; }

        // Foreign key to Semester
        public int SemesterId { get; set; }
        public Semester? Semester { get; set; }

        public DateTime SubmissionDate { get; set; }
        public string? Status { get; set; } // Draft, Submitted, Approved, Rejected
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; } // Soft delete

        public ICollection<Business>? Business { get; set; }
    }
}