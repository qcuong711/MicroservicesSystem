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
        public string? Address { get; set; }
        public string? Note { get; set; }
        public int? CourseClassId { get; set; }
        public string? CourseClassName { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
