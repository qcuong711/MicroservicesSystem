namespace DataManagementApi.Models.Dtos.UserRole
{
    public class UserRoleReadDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string? UserName { get; set; }
        public string? RoleName { get; set; }
        // Thêm các trường khác nếu cần
    }
}
