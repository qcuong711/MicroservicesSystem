using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models
{
    /// <summary>
    /// Matrix-based permission system: Role có quyền gì trên từng Module
    /// Thay thế hệ thống Permission + RolePermission phức tạp cũ
    /// </summary>
    public class RoleModulePermission
    {
        public int Id { get; set; }
        
        [Required]
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;
        
        [Required]
        [StringLength(50)]
        public string ModuleName { get; set; } = string.Empty; // "Thesis", "Student", "Partner", "User", etc.
        
        // Matrix permissions - simple boolean flags
        public bool CanCreate { get; set; }
        public bool CanRead { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
        
        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
} 