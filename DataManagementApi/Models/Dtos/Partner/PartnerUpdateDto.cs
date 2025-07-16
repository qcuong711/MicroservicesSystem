using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.Partner
{
    public class PartnerUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Website { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Partner
    }
}
