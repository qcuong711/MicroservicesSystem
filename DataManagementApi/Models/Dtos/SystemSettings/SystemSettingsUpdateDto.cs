using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.SystemSettings
{
    public class SystemSettingsUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string SettingValue { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
    }
} 