namespace DataManagementApi.Services
{
    /// <summary>
    /// Service để map giữa module names trong matrix permissions và menu paths
    /// </summary>
    public static class ModuleMenuMappingService
    {
        /// <summary>
        /// Mapping giữa module names và menu paths tương ứng
        /// </summary>
        private static readonly Dictionary<string, List<string>> ModuleToMenuPaths = new()
        {
            // Core Management Modules
            { "User", new List<string> { "/users" } },
            { "Role", new List<string> { "/roles", "/permissions" } },
            { "Menu", new List<string> { "/menu" } },
            { "Settings", new List<string> { "/settings" } },
            
            // Dashboard & Reports (Parent + Children)
            { "Dashboard", new List<string> { "/dashboard", "/dashboard/analytics", "/dashboard/reports" } },
            
            // Academic Modules (Individual)
            { "Student", new List<string> { "/students", "/academic/students" } },
            { "Lecturer", new List<string> { "/lecturers", "/academic/lecturers" } },
            { "Department", new List<string> { "/departments", "/academic/departments" } },
            { "CourseClass", new List<string> { "/course-classes", "/academic/course-classes" } },
            
            // Business Modules
            { "Partner", new List<string> { "/partners" } },
            { "Business", new List<string> { "/business" } },
            
            // Academic Management (Parent + Children)
            { "AcademicYear", new List<string> { "/academic-years", "/academic/years" } },
            { "Semester", new List<string> { "/semesters", "/academic/semesters" } },
            { "Academic", new List<string> { "/academic" } }, // Parent menu
            
            // Thesis & Internship
            { "Thesis", new List<string> { "/thesis" } },
            { "ThesisPeriod", new List<string> { "/thesis-periods" } },
            { "InternshipPeriod", new List<string> { "/internship-periods" } },
            { "Internship", new List<string> { "/internship" } },
            
            // Student Modules
            { "StudentThesis", new List<string> { "/student/thesis", "/student/thesis/register", "/student/thesis/progress" } },
            { "StudentInternship", new List<string> { "/student/internship", "/student/internship/register", "/student/internship/progress" } },
            { "StudentProfile", new List<string> { "/student/profile" } },
        };

        /// <summary>
        /// Lấy danh sách menu paths mà user có quyền CanRead
        /// </summary>
        /// <param name="userModulePermissions">List modules mà user có CanRead = true</param>
        /// <returns>List menu paths được phép truy cập</returns>
        public static List<string> GetAccessibleMenuPaths(List<string> userModulePermissions)
        {
            var accessiblePaths = new HashSet<string>();
            
            foreach (var module in userModulePermissions)
            {
                if (ModuleToMenuPaths.TryGetValue(module, out var menuPaths))
                {
                    foreach (var path in menuPaths)
                    {
                        accessiblePaths.Add(path);
                    }
                }
            }
            
            return accessiblePaths.ToList();
        }

        /// <summary>
        /// Kiểm tra user có quyền truy cập menu path không
        /// </summary>
        /// <param name="userModulePermissions">List modules mà user có CanRead = true</param>
        /// <param name="menuPath">Menu path cần kiểm tra</param>
        /// <returns>True nếu có quyền truy cập</returns>
        public static bool HasAccessToMenuPath(List<string> userModulePermissions, string menuPath)
        {
            var accessiblePaths = GetAccessibleMenuPaths(userModulePermissions);
            return accessiblePaths.Contains(menuPath);
        }

        /// <summary>
        /// Lấy tất cả module names có trong mapping
        /// </summary>
        /// <returns>List tất cả module names</returns>
        public static List<string> GetAllModuleNames()
        {
            return ModuleToMenuPaths.Keys.ToList();
        }

        /// <summary>
        /// Lấy tất cả menu paths có trong mapping
        /// </summary>
        /// <returns>List tất cả menu paths</returns>
        public static List<string> GetAllMenuPaths()
        {
            return ModuleToMenuPaths.Values
                .SelectMany(paths => paths)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Lấy module name từ menu path (reverse mapping)
        /// </summary>
        /// <param name="menuPath">Menu path</param>
        /// <returns>Module name tương ứng hoặc null nếu không tìm thấy</returns>
        public static string? GetModuleFromMenuPath(string menuPath)
        {
            foreach (var kvp in ModuleToMenuPaths)
            {
                if (kvp.Value.Contains(menuPath))
                {
                    return kvp.Key;
                }
            }
            return null;
        }
    }
}