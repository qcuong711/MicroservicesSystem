namespace DataManagementApi.Models.Dtos.Student
{
    public class StudentReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime? DeletedAt { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Student
    }
}
