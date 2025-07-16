namespace DataManagementApi.Models.Dtos.PermissionMenu
{
    public class PermissionMenuReadDto
    {
        public int Id { get; set; }
        public int PermissionId { get; set; }
        public int MenuId { get; set; }
        public string? PermissionName { get; set; }
        public string? MenuName { get; set; }
        // Thêm các trường khác nếu cần
    }
}
