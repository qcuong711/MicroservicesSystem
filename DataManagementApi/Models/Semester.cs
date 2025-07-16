namespace DataManagementApi.Models
{
    public class Semester
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AcademicYearId { get; set; }
        public AcademicYear AcademicYear { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? DeletedAt { get; set; }
        // Ensure all fields from DTOs are present and match
        // No additional fields required as all DTO fields are covered
    }
}