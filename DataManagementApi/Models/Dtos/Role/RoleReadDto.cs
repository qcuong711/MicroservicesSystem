namespace DataManagementApi.Models.Dtos.Role
{
    public class RoleReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DeletedAt { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Role
    }
}
