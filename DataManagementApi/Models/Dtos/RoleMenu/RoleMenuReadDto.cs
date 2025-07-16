namespace DataManagementApi.Models.Dtos.RoleMenu
{
    public class RoleMenuReadDto
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int MenuId { get; set; }
        public string? RoleName { get; set; }
        public string? MenuName { get; set; }
        // Thêm các trường khác nếu cần
    }
}
