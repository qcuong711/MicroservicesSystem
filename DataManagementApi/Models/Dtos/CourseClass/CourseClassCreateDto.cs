using System.ComponentModel.DataAnnotations;

namespace DataManagementApi.Models.Dtos.CourseClass
{
    public class CourseClassCreateDto
    {
        [Required(ErrorMessage = "Tên lớp không được để trống")]
        [StringLength(100, ErrorMessage = "Tên lớp không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Khoa không được để trống")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Học kì không được để trống")]
        public int SemesterId { get; set; }

        [Required(ErrorMessage = "Năm học không được để trống")]
        public int AcademicYearId { get; set; }

        public int? AdvisorLecturerId { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}