namespace DataManagementApi.Models.Dtos.Semester
{
    public class SemesterReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int AcademicYearId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? DeletedAt { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Semester
    }
}
