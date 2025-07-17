using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models
{
    public class SystemSettings
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string SettingValue { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
} 