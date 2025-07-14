namespace DataManagementApi.Models.Dtos.Internship
{
    public class InternshipReadDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public int PartnerId { get; set; }
        public string? PartnerName { get; set; }
        public int InternshipPeriodId { get; set; }
        public string? InternshipPeriodName { get; set; }
        public string? ReportUrl { get; set; }
        public double? Grade { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
