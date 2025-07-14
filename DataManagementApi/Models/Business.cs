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
        public bool? IsActive { get; set; } = true;
        // Quan hệ N-N với ThesisPeriod
        public ICollection<ThesisPeriod>? ThesisPeriods { get; set; }
        // Navigation property for N-N with ThesisPeriod
        public ICollection<ThesisPeriodBusiness>? ThesisPeriodBusiness { get; set; }
        // Navigation property for N-N with Partner
        public ICollection<PartnerBusiness>? PartnerBusiness { get; set; }
        public ICollection<Partner>? Partners { get; set; } // Navigation property for related Partners
        // Navigation property for related Thesis (if direct link needed)
        public ICollection<Thesis>? Theses { get; set; }
        // Trường thứ tự hiển thị
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
