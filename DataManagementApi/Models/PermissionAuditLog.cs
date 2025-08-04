using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models
{
    /// <summary>
    /// Audit log cho các hoạt động liên quan đến permissions
    /// </summary>
    public class PermissionAuditLog
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string UserId { get; set; } = string.Empty; // Keycloak User ID
        
        [StringLength(100)]
        public string? UserName { get; set; }
        
        [StringLength(100)]
        public string? UserEmail { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ModuleName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(20)]
        public string Action { get; set; } = string.Empty; // "Create", "Read", "Update", "Delete", etc.
        
        [StringLength(100)]
        public string? Resource { get; set; } // Specific resource being accessed (e.g., User ID, Thesis ID)
        
        public bool PermissionGranted { get; set; }
        
        [StringLength(500)]
        public string? DeniedReason { get; set; }
        
        [StringLength(200)]
        public string? IpAddress { get; set; }
        
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        [StringLength(100)]
        public string? RequestPath { get; set; }
        
        [StringLength(10)]
        public string? HttpMethod { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Additional context data as JSON
        [StringLength(2000)]
        public string? AdditionalData { get; set; }
        
        // Severity levels for filtering
        public AuditSeverity Severity { get; set; } = AuditSeverity.Info;
    }
    
    public enum AuditSeverity
    {
        Info = 0,       // Normal successful operations
        Warning = 1,    // Denied access attempts
        Error = 2,      // System errors during permission check
        Critical = 3    // Security violations or suspicious activity
    }
} 