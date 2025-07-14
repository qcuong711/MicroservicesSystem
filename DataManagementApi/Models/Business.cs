using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataManagementApi.Models
{
    [Table("Business")]
    public class Business
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        // Quan hệ N-N với ThesisPeriod
        public ICollection<ThesisPeriod>? ThesisPeriods { get; set; }
        public ICollection<ThesisPeriodBusiness>? ThesisPeriodBusiness { get; set; }
        public ICollection<PartnerBusiness>? PartnerBusiness { get; set; }
        public ICollection<Partner>? Partners { get; set; } // Navigation property for related Partners
        // Trường thứ tự hiển thị
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
