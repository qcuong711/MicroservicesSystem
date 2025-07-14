namespace DataManagementApi.Models.Dtos.User
{
    public class UserReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KeycloakUserId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public List<string> UserRoles { get; set; } = new List<string>();
        // Giữ các trường cũ, bổ sung đầy đủ field theo model User
    }
}
