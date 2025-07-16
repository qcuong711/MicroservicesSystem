namespace DataManagementApi.Models.Dtos.AcademicYear
{
    public class AcademicYearReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model AcademicYear
    }
}
