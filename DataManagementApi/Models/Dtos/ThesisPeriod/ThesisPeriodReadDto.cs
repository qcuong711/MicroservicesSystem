namespace DataManagementApi.Models.Dtos.ThesisPeriod
{
    public class ThesisPeriodReadDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int AcademicYearId { get; set; }
        public string? AcademicYearName { get; set; }
        public int SemesterId { get; set; } // Thêm dòng này
        public string? SemesterName { get; set; } // Nếu muốn hiển thị tên học kỳ
    }
}
