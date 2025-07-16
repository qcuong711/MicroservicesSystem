namespace DataManagementApi.Models.Dtos.Partner
{
    public class PartnerReadDto
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
        public DateTime? DeletedAt { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Partner
        public string TaxCode { get; set; } = string.Empty;
        public string? BankAccount { get; set; }
        public string? BankName { get; set; }
        public string? BranchName { get; set; }
        public string? SwiftCode { get; set; }
        public string? Note { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
