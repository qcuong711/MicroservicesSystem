namespace DataManagementApi.Models.Dtos.RolePermission
{
    public class RolePermissionReadDto
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public string? RoleName { get; set; }
        public string? PermissionName { get; set; }
        // Thêm các trường khác nếu cần
    }
}
