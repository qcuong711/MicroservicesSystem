namespace DataManagementApi.Models.Dtos.Menu
{
    public class MenuReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public List<MenuChildDto> ChildMenus { get; set; } = new();
        public DateTime? DeletedAt { get; set; }
        // Giữ các trường cũ, bổ sung đầy đủ field theo model Menu
    }

    public class MenuChildDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public int? ParentId { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
