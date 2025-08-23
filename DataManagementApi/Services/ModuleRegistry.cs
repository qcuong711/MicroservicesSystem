using DataManagementApi.Models;

namespace DataManagementApi.Services
{
    /// <summary>
    /// Centralized Module Registry để quản lý tất cả modules trong hệ thống
    /// </summary>
    public class ModuleDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> ApiPaths { get; set; } = new();
        public List<string> MenuPaths { get; set; } = new();
        public List<string> AvailablePermissions { get; set; } = new();
        public string Category { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public static class ModuleRegistry
    {
        private static readonly Dictionary<string, ModuleDefinition> _modules = new();
        
        static ModuleRegistry()
        {
            InitializeModules();
        }

        public static Dictionary<string, ModuleDefinition> Modules => _modules;

        public static ModuleDefinition? GetModule(string moduleName)
        {
            return _modules.TryGetValue(moduleName, out var module) ? module : null;
        }

        public static List<ModuleDefinition> GetModulesByCategory(string category)
        {
            return _modules.Values
                .Where(m => m.Category == category && m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ToList();
        }

        public static List<string> GetModuleNames()
        {
            return _modules.Keys.Where(k => _modules[k].IsActive).OrderBy(k => k).ToList();
        }

        public static string? GetModuleByApiPath(string apiPath)
        {
            var module = _modules.Values
                .FirstOrDefault(m => m.ApiPaths.Any(path => apiPath.StartsWith(path, StringComparison.OrdinalIgnoreCase)));
            return module?.Name;
        }

        public static List<string> GetMenuPathsForModule(string moduleName)
        {
            return GetModule(moduleName)?.MenuPaths ?? new List<string>();
        }

        private static void InitializeModules()
        {
            // Core Management
            _modules["User"] = new ModuleDefinition
            {
                Name = "User",
                DisplayName = "Quản lý người dùng",
                Description = "Quản lý tài khoản người dùng trong hệ thống",
                ApiPaths = ["/api/users"],
                MenuPaths = ["/users"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete", "ManageRoles"],
                Category = "Core",
                DisplayOrder = 1
            };

            _modules["Role"] = new ModuleDefinition
            {
                Name = "Role",
                DisplayName = "Quản lý vai trò",
                Description = "Quản lý vai trò và phân quyền",
                ApiPaths = ["/api/roles", "/api/permissions", "/api/role-module-permissions", "/api/modules"],
                MenuPaths = ["/roles", "/permissions"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete"],
                Category = "Core",
                DisplayOrder = 2
            };

            _modules["Menu"] = new ModuleDefinition
            {
                Name = "Menu",
                DisplayName = "Quản lý menu",
                Description = "Cấu hình menu và điều hướng",
                ApiPaths = ["/api/menus"],
                MenuPaths = ["/menu"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete"],
                Category = "Core",
                DisplayOrder = 3
            };

            _modules["Settings"] = new ModuleDefinition
            {
                Name = "Settings",
                DisplayName = "Cài đặt hệ thống",
                Description = "Cấu hình các thông số hệ thống",
                ApiPaths = ["/api/system-settings"],
                MenuPaths = ["/settings"],
                AvailablePermissions = ["Read", "Update"],
                Category = "Core",
                DisplayOrder = 4
            };

            // Academic Management
            _modules["Student"] = new ModuleDefinition
            {
                Name = "Student",
                DisplayName = "Quản lý sinh viên",
                Description = "Quản lý thông tin sinh viên",
                ApiPaths = ["/api/students"],
                MenuPaths = ["/students", "/academic/students"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete", "Export"],
                Category = "Academic",
                DisplayOrder = 10
            };

            _modules["Lecturer"] = new ModuleDefinition
            {
                Name = "Lecturer",
                DisplayName = "Quản lý giảng viên",
                Description = "Quản lý thông tin giảng viên",
                ApiPaths = ["/api/lecturers"],
                MenuPaths = ["/lecturers"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete", "Assign"],
                Category = "Academic",
                DisplayOrder = 11
            };

            _modules["Department"] = new ModuleDefinition
            {
                Name = "Department",
                DisplayName = "Quản lý khoa/phòng ban",
                Description = "Quản lý cơ cấu tổ chức khoa/phòng ban",
                ApiPaths = ["/api/departments"],
                MenuPaths = ["/departments", "/academic/departments"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete"],
                Category = "Academic",
                DisplayOrder = 12
            };

            _modules["AcademicYear"] = new ModuleDefinition
            {
                Name = "AcademicYear",
                DisplayName = "Năm học",
                Description = "Quản lý năm học",
                ApiPaths = ["/api/academic-years"],
                MenuPaths = ["/academic-years", "/academic/years"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete"],
                Category = "Academic",
                DisplayOrder = 13
            };

            _modules["Semester"] = new ModuleDefinition
            {
                Name = "Semester",
                DisplayName = "Học kỳ",
                Description = "Quản lý học kỳ",
                ApiPaths = ["/api/semesters"],
                MenuPaths = ["/semesters", "/academic/semesters"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete"],
                Category = "Academic",
                DisplayOrder = 14
            };

            _modules["CourseClass"] = new ModuleDefinition
            {
                Name = "CourseClass",
                DisplayName = "Lớp học phần",
                Description = "Quản lý lớp học phần",
                ApiPaths = ["/api/courseclass"],
                MenuPaths = ["/course-classes", "/academic/course-classes"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete", "Assign"],
                Category = "Academic",
                DisplayOrder = 15
            };

            // Business & Partners
            _modules["Partner"] = new ModuleDefinition
            {
                Name = "Partner",
                DisplayName = "Đối tác",
                Description = "Quản lý đối tác hợp tác",
                ApiPaths = ["/api/partners"],
                MenuPaths = ["/partners"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete", "Export"],
                Category = "Business",
                DisplayOrder = 20
            };

            _modules["Business"] = new ModuleDefinition
            {
                Name = "Business",
                DisplayName = "Doanh nghiệp",
                Description = "Quản lý thông tin doanh nghiệp",
                ApiPaths = ["/api/businesses"],
                MenuPaths = ["/business"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete", "Export"],
                Category = "Business",
                DisplayOrder = 21
            };

            // Thesis & Internship
            _modules["Thesis"] = new ModuleDefinition
            {
                Name = "Thesis",
                DisplayName = "Đồ án/Luận văn",
                Description = "Quản lý đồ án và luận văn",
                ApiPaths = ["/api/theses"],
                MenuPaths = ["/thesis"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete", "Approve", "Export"],
                Category = "Project",
                DisplayOrder = 30
            };

            _modules["ThesisPeriod"] = new ModuleDefinition
            {
                Name = "ThesisPeriod",
                DisplayName = "Kỳ đồ án",
                Description = "Quản lý các kỳ làm đồ án",
                ApiPaths = ["/api/thesis-periods"],
                MenuPaths = ["/thesis-periods"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete"],
                Category = "Project",
                DisplayOrder = 31
            };

            _modules["InternshipPeriod"] = new ModuleDefinition
            {
                Name = "InternshipPeriod",
                DisplayName = "Kỳ thực tập",
                Description = "Quản lý các kỳ thực tập",
                ApiPaths = ["/api/internship-periods"],
                MenuPaths = ["/internship-periods"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete"],
                Category = "Project",
                DisplayOrder = 32
            };
            
            _modules["Internship"] = new ModuleDefinition
            {
                Name = "Internship",
                DisplayName = "Thực tập",
                Description = "Quản lý thực tập",
                ApiPaths = ["/api/internships"],
                MenuPaths = ["/internship"],
                AvailablePermissions = ["Create", "Read", "Update", "Delete", "Approve", "Export"],
                Category = "Project",
                DisplayOrder = 33
            };

            // Dashboard & Reports
            _modules["Dashboard"] = new ModuleDefinition
            {
                Name = "Dashboard",
                DisplayName = "Bảng điều khiển",
                Description = "Trang tổng quan và báo cáo",
                ApiPaths = ["/api/dashboard", "/api/reports"],
                MenuPaths = ["/dashboard", "/dashboard/analytics", "/dashboard/reports"],
                AvailablePermissions = ["Read", "Export"],
                Category = "Dashboard",
                DisplayOrder = 0
            };

            // Parent menu modules
            _modules["Academic"] = new ModuleDefinition
            {
                Name = "Academic",
                DisplayName = "Học vụ",
                Description = "Menu cha cho các chức năng học vụ",
                ApiPaths = [],
                MenuPaths = ["/academic"],
                AvailablePermissions = ["Read"],
                Category = "Parent",
                DisplayOrder = 100
            };
            
            // Student Module - Thêm mới
            _modules["StudentThesis"] = new ModuleDefinition
            {
                Name = "StudentThesis",
                DisplayName = "Đồ án sinh viên",
                Description = "Quản lý đồ án của sinh viên",
                ApiPaths = ["/api/student-theses"],
                MenuPaths = ["/student/thesis", "/student/thesis/register", "/student/thesis/progress"],
                AvailablePermissions = ["Create", "Read", "Update", "Submit", "View"],
                Category = "Student",
                DisplayOrder = 40
            };
            
            _modules["StudentInternship"] = new ModuleDefinition
            {
                Name = "StudentInternship",
                DisplayName = "Thực tập sinh viên",
                Description = "Quản lý thực tập của sinh viên",
                ApiPaths = ["/api/student-internships"],
                MenuPaths = ["/student/internship", "/student/internship/register", "/student/internship/progress"],
                AvailablePermissions = ["Create", "Read", "Update", "Submit", "View"],
                Category = "Student",
                DisplayOrder = 41
            };
            
            _modules["StudentProfile"] = new ModuleDefinition
            {
                Name = "StudentProfile",
                DisplayName = "Hồ sơ sinh viên",
                Description = "Quản lý thông tin cá nhân sinh viên",
                ApiPaths = ["/api/student-profile"],
                MenuPaths = ["/student/profile"],
                AvailablePermissions = ["Read", "Update"],
                Category = "Student",
                DisplayOrder = 42
            };
        }
    }
}