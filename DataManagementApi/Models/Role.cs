namespace DataManagementApi.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? DeletedAt { get; set; }
        
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
        
        // New matrix-based permission system
        public ICollection<RoleModulePermission> RoleModulePermissions { get; set; } = new List<RoleModulePermission>();
    }
} 