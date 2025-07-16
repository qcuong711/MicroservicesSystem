namespace DataManagementApi.Models.Dtos.Department
{
    public class DepartmentReadDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? ParentDepartmentId { get; set; }
        public string? ParentDepartmentName { get; set; }
        public List<DepartmentChildDto> ChildDepartments { get; set; } = new();
        public DateTime? DeletedAt { get; set; }
    }

    public class DepartmentChildDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int? ParentDepartmentId { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
