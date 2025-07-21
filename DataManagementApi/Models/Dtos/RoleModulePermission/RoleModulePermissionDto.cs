using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.RoleModulePermission
{
    /// <summary>
    /// DTO cho read operations của RoleModulePermission
    /// </summary>
    public class RoleModulePermissionDto
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public bool CanCreate { get; set; }
        public bool CanRead { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>
    /// DTO cho toàn bộ matrix của 1 role
    /// </summary>
    public class RolePermissionMatrixDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<ModulePermissionDto> Modules { get; set; } = new();
    }

    /// <summary>
    /// DTO cho permissions của 1 module
    /// </summary>
    public class ModulePermissionDto
    {
        [Required]
        [StringLength(50)]
        public string ModuleName { get; set; } = string.Empty;
        public bool CanCreate { get; set; }
        public bool CanRead { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }

    /// <summary>
    /// DTO cho bulk update toàn bộ matrix của 1 role
    /// </summary>
    public class UpdateRolePermissionMatrixDto
    {
        [Required]
        public int RoleId { get; set; }
        
        [Required]
        public List<ModulePermissionDto> Modules { get; set; } = new();
    }

    /// <summary>
    /// DTO cho create/update individual module permission
    /// </summary>
    public class CreateRoleModulePermissionDto
    {
        [Required]
        public int RoleId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ModuleName { get; set; } = string.Empty;
        
        public bool CanCreate { get; set; }
        public bool CanRead { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }
} 