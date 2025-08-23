using DataManagementApi.Data;
using DataManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;

        public DataSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            // Seed Academic Years
            if (!await _context.AcademicYears.AnyAsync())
            {
                var academicYears = new[]
                {
                    new AcademicYear { Name = "2024-2025", StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2025, 8, 31) },
                    new AcademicYear { Name = "2025-2026", StartDate = new DateTime(2025, 9, 1), EndDate = new DateTime(2026, 8, 31) }
                };

                await _context.AcademicYears.AddRangeAsync(academicYears);
                await _context.SaveChangesAsync();
            }

            // Seed Departments
            if (!await _context.Departments.AnyAsync())
            {
                var departments = new[]
                {
                    new Department { Name = "Khoa Công nghệ Thông tin", Code = "CNTT" },
                    new Department { Name = "Khoa Kinh tế", Code = "KT" },
                    new Department { Name = "Khoa Kỹ thuật", Code = "KT" }
                };

                await _context.Departments.AddRangeAsync(departments);
                await _context.SaveChangesAsync();
            }

            // Seed Semesters
            if (!await _context.Semesters.AnyAsync())
            {
                var firstAcademicYear = await _context.AcademicYears.FirstAsync();
                var semesters = new[]
                {
                    new Semester { Name = "Học kỳ 1", AcademicYearId = firstAcademicYear.Id },
                    new Semester { Name = "Học kỳ 2", AcademicYearId = firstAcademicYear.Id },
                    new Semester { Name = "Học kỳ hè", AcademicYearId = firstAcademicYear.Id }
                };

                await _context.Semesters.AddRangeAsync(semesters);
                await _context.SaveChangesAsync();
            }

            // Seed Students
            if (!await _context.Students.AnyAsync())
            {
                var students = new[]
                {
                    new Student 
                    { 
                        StudentCode = "SV001", 
                        FullName = "Nguyễn Văn A", 
                        DateOfBirth = new DateTime(2000, 1, 15),
                        Email = "nguyenvana@example.com",
                        PhoneNumber = "0123456789"
                    },
                    new Student 
                    { 
                        StudentCode = "SV002", 
                        FullName = "Trần Thị B", 
                        DateOfBirth = new DateTime(2000, 5, 20),
                        Email = "tranthib@example.com",
                        PhoneNumber = "0987654321"
                    },
                    new Student 
                    { 
                        StudentCode = "SV003", 
                        FullName = "Lê Hoàng C", 
                        DateOfBirth = new DateTime(2000, 8, 10),
                        Email = "lehoangc@example.com",
                        PhoneNumber = "0345678901"
                    }
                };

                await _context.Students.AddRangeAsync(students);
                await _context.SaveChangesAsync();
            }

            // Seed Partners
            if (!await _context.Partners.AnyAsync())
            {
                var partners = new[]
                {
                    new Partner
                    {
                        Name = "Công ty ABC",
                        Address = "123 Đường ABC, Quận 1, TP.HCM",
                        PhoneNumber = "0123456789",
                        Email = "contact@abc.com"
                    },
                    new Partner
                    {
                        Name = "Công ty XYZ",
                        Address = "456 Đường XYZ, Quận 2, TP.HCM",
                        PhoneNumber = "0987654321",
                        Email = "info@xyz.com"
                    }
                };

                await _context.Partners.AddRangeAsync(partners);
                await _context.SaveChangesAsync();
            }

            // Seed Roles
            if (!await _context.Roles.AnyAsync())
            {
                var roles = new[]
                {
                    new Role { Name = "Admin" },
                    new Role { Name = "Teacher" },
                    new Role { Name = "Student" }
                };

                await _context.Roles.AddRangeAsync(roles);
                await _context.SaveChangesAsync();
            }

            // Seed Permissions
            if (!await _context.Permissions.AnyAsync())
            {
                var permissions = new[]
                {
                    new Permission { Name = "CREATE_THESIS", Module = "Thesis" },
                    new Permission { Name = "READ_THESIS", Module = "Thesis" },
                    new Permission { Name = "UPDATE_THESIS", Module = "Thesis" },
                    new Permission { Name = "DELETE_THESIS", Module = "Thesis" },
                    new Permission { Name = "MANAGE_STUDENTS", Module = "Student" },
                    new Permission { Name = "MANAGE_PARTNERS", Module = "Partner" }
                };

                await _context.Permissions.AddRangeAsync(permissions);
                await _context.SaveChangesAsync();
            }

            // Seed Menus
            if (!await _context.Menus.AnyAsync())
            {
                // Main menu items (based on existing pages)
                var mainMenus = new[]
                {
                    new Menu { Name = "Người dùng", Path = "/users", Icon = "users", DisplayOrder = 2 },
                    new Menu { Name = "Vai trò", Path = "/roles", Icon = "shield", DisplayOrder = 3 },
                    new Menu { Name = "Quyền", Path = "/permissions", Icon = "lock", DisplayOrder = 4 },
                    new Menu { Name = "Khóa luận", Path = "/thesis", Icon = "book-open", DisplayOrder = 5 },
                    new Menu { Name = "Thực tập", Path = "/internship", Icon = "briefcase", DisplayOrder = 6 },
                    new Menu { Name = "Doanh nghiệp", Path = "/partners", Icon = "building2", DisplayOrder = 7 },
                    new Menu { Name = "Cài đặt", Path = "/settings", Icon = "settings", DisplayOrder = 9 },
                    new Menu { Name = "Menu", Path = "/menu", Icon = "menu", DisplayOrder = 99 } // Always available for admin
                };

                await _context.Menus.AddRangeAsync(mainMenus);
                await _context.SaveChangesAsync();

                // Create Dashboard parent menu with children
                var dashboardParent = new Menu 
                { 
                    Name = "Tổng quan", 
                    Path = "/dashboard", 
                    Icon = "home", 
                    DisplayOrder = 1,
                    ParentId = null
                };
                
                await _context.Menus.AddAsync(dashboardParent);
                await _context.SaveChangesAsync();

                // Add dashboard child menus (based on existing dashboard pages)
                var dashboardChildMenus = new[]
                {
                    new Menu { Name = "Phân tích", Path = "/dashboard/analytics", Icon = "line-chart", DisplayOrder = 1, ParentId = dashboardParent.Id },
                    new Menu { Name = "Báo cáo", Path = "/dashboard/reports", Icon = "bar-chart", DisplayOrder = 2, ParentId = dashboardParent.Id }
                };

                await _context.Menus.AddRangeAsync(dashboardChildMenus);
                await _context.SaveChangesAsync();

                // Create Academic management parent menu with children
                var academicParent = new Menu 
                { 
                    Name = "Quản lý đào tạo", 
                    Path = "/academic", 
                    Icon = "graduation-cap", 
                    DisplayOrder = 8,
                    ParentId = null
                };
                
                await _context.Menus.AddAsync(academicParent);
                await _context.SaveChangesAsync();

                // Add academic child menus (based on existing academic pages)
                var academicChildMenus = new[]
                {
                    new Menu { Name = "Năm học", Path = "/academic/years", Icon = "calendar", DisplayOrder = 1, ParentId = academicParent.Id },
                    new Menu { Name = "Học kỳ", Path = "/academic/semesters", Icon = "calendar-days", DisplayOrder = 2, ParentId = academicParent.Id },
                    new Menu { Name = "Khoa & Chuyên ngành", Path = "/academic/departments", Icon = "building", DisplayOrder = 3, ParentId = academicParent.Id },
                    new Menu { Name = "Sinh viên", Path = "/academic/students", Icon = "user-graduate", DisplayOrder = 4, ParentId = academicParent.Id },
                    new Menu { Name = "Giảng viên", Path = "/academic/lecturers", Icon = "user-check", DisplayOrder = 5, ParentId = academicParent.Id },
                    new Menu { Name = "Lớp học phần", Path = "/academic/course-classes", Icon = "users", DisplayOrder = 6, ParentId = academicParent.Id }
                };

                await _context.Menus.AddRangeAsync(academicChildMenus);
                await _context.SaveChangesAsync();
            }

            // Seed RoleMenus (gán menu cho roles)
            if (!await _context.RoleMenus.AnyAsync())
            {
                var adminRole = await _context.Roles.FirstAsync(r => r.Name == "Admin");
                var teacherRole = await _context.Roles.FirstAsync(r => r.Name == "Teacher");
                var studentRole = await _context.Roles.FirstAsync(r => r.Name == "Student");

                var allMenus = await _context.Menus.ToListAsync();

                // Admin có quyền truy cập tất cả menu
                var adminRoleMenus = allMenus.Select(m => new RoleMenu
                {
                    RoleId = adminRole.Id,
                    MenuId = m.Id
                }).ToList();

                // Teacher có quyền truy cập một số menu
                var teacherMenus = allMenus.Where(m => 
                    m.Name.Contains("Tổng quan") || 
                    m.Name.Contains("Khóa luận") || 
                    m.Name.Contains("Thực tập") || 
                    m.Name.Contains("Sinh viên") ||
                    m.Name.Contains("Giảng viên") ||
                    m.Name.Contains("Lớp học phần") ||
                    m.Name.Contains("Phân tích") ||
                    m.Name.Contains("Báo cáo") ||
                    m.Name.Contains("Quản lý đào tạo") ||
                    m.Name.Contains("Năm học") ||
                    m.Name.Contains("Học kỳ") ||
                    m.Name.Contains("Khoa & Chuyên ngành")
                ).ToList();

                var teacherRoleMenus = teacherMenus.Select(m => new RoleMenu
                {
                    RoleId = teacherRole.Id,
                    MenuId = m.Id
                }).ToList();

                // Tạo menu sinh viên mới
                var studentParent = new Menu 
                { 
                    Name = "Sinh viên", 
                    Path = "/student", 
                    Icon = "user-graduate", 
                    DisplayOrder = 10,
                    ParentId = null
                };
                
                await _context.Menus.AddAsync(studentParent);
                await _context.SaveChangesAsync();
                
                // Thêm menu con cho sinh viên
                var studentChildMenus = new[]
                {
                    new Menu { Name = "Đồ án", Path = "/student/thesis", Icon = "book", DisplayOrder = 1, ParentId = studentParent.Id },
                    new Menu { Name = "Đăng ký đề tài", Path = "/student/thesis/register", Icon = "edit", DisplayOrder = 2, ParentId = studentParent.Id },
                    new Menu { Name = "Tiến độ đồ án", Path = "/student/thesis/progress", Icon = "chart-line", DisplayOrder = 3, ParentId = studentParent.Id },
                    new Menu { Name = "Thực tập", Path = "/student/internship", Icon = "briefcase", DisplayOrder = 4, ParentId = studentParent.Id },
                    new Menu { Name = "Đăng ký thực tập", Path = "/student/internship/register", Icon = "edit", DisplayOrder = 5, ParentId = studentParent.Id },
                    new Menu { Name = "Tiến độ thực tập", Path = "/student/internship/progress", Icon = "chart-line", DisplayOrder = 6, ParentId = studentParent.Id },
                    new Menu { Name = "Hồ sơ cá nhân", Path = "/student/profile", Icon = "user", DisplayOrder = 7, ParentId = studentParent.Id }
                };
                
                await _context.Menus.AddRangeAsync(studentChildMenus);
                await _context.SaveChangesAsync();
                
                // Cập nhật danh sách menu
                allMenus = await _context.Menus.ToListAsync();
                
                // Student có quyền truy cập một số menu cơ bản
                var studentMenus = allMenus.Where(m => 
                    m.Name.Contains("Tổng quan") || 
                    m.Name.Contains("Khóa luận") || 
                    m.Name.Contains("Thực tập") ||
                    m.Name.Contains("Phân tích") ||
                    m.Path.StartsWith("/student")
                ).ToList();

                var studentRoleMenus = studentMenus.Select(m => new RoleMenu
                {
                    RoleId = studentRole.Id,
                    MenuId = m.Id
                }).ToList();

                // Thêm tất cả RoleMenus
                await _context.RoleMenus.AddRangeAsync(adminRoleMenus);
                await _context.RoleMenus.AddRangeAsync(teacherRoleMenus);
                await _context.RoleMenus.AddRangeAsync(studentRoleMenus);
                await _context.SaveChangesAsync();
            }
            
            // Initialize system settings
            await InitializeSystemSettings();
        }
        
        private async Task InitializeSystemSettings()
        {
            // Initialize default system settings
            var settingKey = "ALLOW_CROSS_DEPARTMENT_DATA_ACCESS";
            var existingSetting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == settingKey);

            if (existingSetting == null)
            {
                var defaultSetting = new SystemSettings
                {
                    SettingKey = settingKey,
                    SettingValue = "false",
                    Description = "Cho phép các phòng ban xem dữ liệu của phòng ban khác",
                    CreatedAt = DateTime.UtcNow
                };

                _context.SystemSettings.Add(defaultSetting);
                await _context.SaveChangesAsync();
            }
        }
    }
}
