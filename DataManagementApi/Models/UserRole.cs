namespace DataManagementApi.Models
{
    // Bảng nối cho quan hệ nhiều-nhiều giữa User và Role
    public class UserRole
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
    }
}