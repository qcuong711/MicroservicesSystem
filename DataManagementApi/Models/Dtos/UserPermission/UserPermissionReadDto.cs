namespace DataManagementApi.Models.Dtos.UserPermission
{
    public class UserPermissionReadDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int PermissionId { get; set; }
        public string? UserName { get; set; }
        public string? PermissionName { get; set; }
        // Thêm các trường khác nếu cần
    }
}
