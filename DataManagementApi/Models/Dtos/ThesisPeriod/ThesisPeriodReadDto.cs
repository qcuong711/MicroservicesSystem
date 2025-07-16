namespace DataManagementApi.Models.Dtos.ThesisPeriod
{
    public class ThesisPeriodReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        // Thêm các trường khác nếu cần
    }
}
