using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models
{
    public class Partner
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? DeletedAt { get; set; } // For soft delete

        public ICollection<PartnerBusiness>? PartnerBusiness { get; set; }
        public ICollection<Business>? Business { get; set; }
    }
}