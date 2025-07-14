namespace DataManagementApi.Models.Dtos.Lecturer
{
    public class LecturerReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LecturerCode { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AcademicRank { get; set; }
        public string? Degree { get; set; }
        public string? Specialization { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        // Thêm các trường khác nếu cần
    }
}
