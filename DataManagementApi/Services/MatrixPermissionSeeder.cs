using DataManagementApi.Data;
using DataManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Services
{
    public class MatrixPermissionSeeder
    {
        private readonly ApplicationDbContext _context;

        public MatrixPermissionSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SeedMatrixPermissionsAsync()
        {
            // Clear existing matrix permissions để re-seed với modules mới
            await ClearExistingMatrixPermissionsAsync();

            // Lấy roles hiện có (fix role names để match với DataSeeder)
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var lecturerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Teacher");
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");

            if (adminRole == null || lecturerRole == null || studentRole == null)
            {
                return; // Chưa có roles
            }

            var modules = ModuleRegistry.GetModuleNames();

            var matrixPermissions = new List<RoleModulePermission>();

            // ADMIN - Full permissions cho tất cả modules
            foreach (var module in modules)
            {
                matrixPermissions.Add(new RoleModulePermission
                {
                    RoleId = adminRole.Id,
                    ModuleName = module,
                    CanCreate = true,
                    CanRead = true,
                    CanUpdate = true,
                    CanDelete = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // LECTURER - Limited permissions
            var lecturerModules = new Dictionary<string, (bool create, bool read, bool update, bool delete)>
            {
                { "Dashboard", (false, true, false, false) },    // Xem dashboard & reports
                { "Thesis", (false, true, true, false) },        // Có thể xem và sửa thesis
                { "Student", (false, true, false, false) },      // Chỉ xem student
                { "Partner", (true, true, true, false) },        // Quản lý partners
                { "Department", (false, true, false, false) },   // Xem departments
                { "Lecturer", (false, true, true, false) },      // Xem và update profile
                { "Academic", (false, true, false, false) },     // Xem academic parent menu
                { "AcademicYear", (false, true, false, false) }, // Xem academic years
                { "Semester", (false, true, false, false) },     // Xem semesters
                { "InternshipPeriod", (false, true, false, false) }, // Xem internship periods
                { "ThesisPeriod", (false, true, false, false) }  // Xem thesis periods
            };

            foreach (var (module, permissions) in lecturerModules)
            {
                matrixPermissions.Add(new RoleModulePermission
                {
                    RoleId = lecturerRole.Id,
                    ModuleName = module,
                    CanCreate = permissions.create,
                    CanRead = permissions.read,
                    CanUpdate = permissions.update,
                    CanDelete = permissions.delete,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // STUDENT - Very limited permissions
            var studentModules = new Dictionary<string, (bool create, bool read, bool update, bool delete)>
            {
                { "Dashboard", (false, true, false, false) },  // Xem dashboard cơ bản
                { "Thesis", (true, true, true, false) },     // Có thể tạo, xem, sửa thesis của mình
                { "Student", (false, true, true, false) },   // Xem và update profile
                { "Partner", (false, true, false, false) },  // Chỉ xem partners
                { "Department", (false, true, false, false) }, // Xem departments
                { "AcademicYear", (false, true, false, false) }, // Xem academic years
                { "Semester", (false, true, false, false) },   // Xem semesters
                { "ThesisPeriod", (false, true, false, false) } // Xem thesis periods
            };

            foreach (var (module, permissions) in studentModules)
            {
                matrixPermissions.Add(new RoleModulePermission
                {
                    RoleId = studentRole.Id,
                    ModuleName = module,
                    CanCreate = permissions.create,
                    CanRead = permissions.read,
                    CanUpdate = permissions.update,
                    CanDelete = permissions.delete,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Save to database
            await _context.RoleModulePermissions.AddRangeAsync(matrixPermissions);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Clear existing matrix permissions để re-seed
        /// </summary>
        private async Task ClearExistingMatrixPermissionsAsync()
        {
            var existingPermissions = await _context.RoleModulePermissions.ToListAsync();
            if (existingPermissions.Any())
            {
                _context.RoleModulePermissions.RemoveRange(existingPermissions);
                await _context.SaveChangesAsync();
            }
        }
    }
} 