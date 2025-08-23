using DataManagementApi.Models.Dtos.Lecturer;


namespace DataManagementApi.Models.Dtos.Thesis
{
    public class ThesisReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? StudentCode { get; set; }
        public int SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public string? SupervisorEmail { get; set; }
        public List<SupervisorDto>? Supervisors { get; set; } // Danh sách giảng viên hướng dẫn
        public int? ExaminerId { get; set; }
        public string? ExaminerName { get; set; }
        public string? ExaminerEmail { get; set; }
        public List<ExaminerDto>? Examiners { get; set; } // Danh sách giảng viên phản biện
        public int ThesisPeriodId { get; set; }
        public string? ThesisPeriodName { get; set; }
        public int AcademicYearId { get; set; }
        public string? AcademicYearName { get; set; }
        public int SemesterId { get; set; }
        public string? SemesterName { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? Status { get; set; } // Draft, Submitted, Approved, Rejected
        
        // File information
        public string? ReportUrl { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public long? FileSize { get; set; }
        public DateTime? UploadDate { get; set; }
        
        // Score information
        public decimal? Score { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public LecturerReadDto? Supervisor { get; set; }
        public LecturerReadDto? Examiner { get; set; }
    }
    
    public class SupervisorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
    
    public class ExaminerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
