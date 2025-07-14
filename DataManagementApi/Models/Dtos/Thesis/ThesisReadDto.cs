namespace DataManagementApi.Models.Dtos.Thesis
{
    public class ThesisReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public int SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public int? ExaminerId { get; set; }
        public string? ExaminerName { get; set; }
        public int ThesisPeriodId { get; set; }
        public string? ThesisPeriodName { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        // Thêm các trường khác nếu cần
    }
}
