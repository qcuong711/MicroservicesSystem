namespace DataManagementApi.Models.Dtos.UserMenu
{
    public class UserMenuReadDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MenuId { get; set; }
        public string? UserName { get; set; }
        public string? MenuName { get; set; }
        // Thêm các trường khác nếu cần
    }
}
