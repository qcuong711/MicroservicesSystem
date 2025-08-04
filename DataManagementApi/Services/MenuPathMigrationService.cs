using DataManagementApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Services
{
    /// <summary>
    /// Service để migrate menu paths từ /admin prefix sang non-prefix paths
    /// </summary>
    public class MenuPathMigrationService
    {
        private readonly ApplicationDbContext _context;

        public MenuPathMigrationService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Migrate tất cả menu paths có /admin prefix thành non-prefix paths
        /// </summary>
        public async Task MigrateMenuPathsAsync()
        {
            try
            {
                // Lấy tất cả menus có path bắt đầu với /admin
                var menusToUpdate = await _context.Menus
                    .Where(m => m.Path.StartsWith("/admin/"))
                    .ToListAsync();

                if (!menusToUpdate.Any())
                {
                    return;
                }

                // Dictionary mapping old paths to new paths
                var pathMappings = new Dictionary<string, string>
                {
                    { "/admin/users", "/users" },
                    { "/admin/roles", "/roles" },
                    { "/admin/permissions", "/permissions" },
                    { "/admin/menu", "/menu" },
                    { "/admin/settings", "/settings" },
                    { "/admin/students", "/students" },
                    { "/admin/lecturers", "/lecturers" },
                    { "/admin/departments", "/departments" },
                    { "/admin/partners", "/partners" },
                    { "/admin/business", "/business" },
                    { "/admin/academic-years", "/academic-years" },
                    { "/admin/semesters", "/semesters" },
                    { "/admin/thesis", "/thesis" },
                    { "/admin/thesis-periods", "/thesis-periods" },
                    { "/admin/internship", "/internship" },
                    { "/admin/internship-periods", "/internship-periods" },
                    // Academic child paths
                    { "/admin/academic/years", "/academic-years" },
                    { "/admin/academic/semesters", "/semesters" },
                    { "/admin/academic/departments", "/departments" },
                    { "/admin/academic/students", "/students" },
                    // Dashboard child paths
                    { "/admin/dashboard/analytics", "/dashboard/analytics" },
                    { "/admin/dashboard/reports", "/dashboard/reports" }
                };

                foreach (var menu in menusToUpdate)
                {
                    var oldPath = menu.Path;
                    
                    // Check if we have a direct mapping
                    if (pathMappings.TryGetValue(oldPath, out var newPath))
                    {
                        menu.Path = newPath;
                    }
                    else
                    {
                        // For unmapped paths, just remove /admin prefix
                        if (oldPath.StartsWith("/admin/"))
                        {
                            newPath = oldPath.Substring(6); // Remove "/admin" (6 characters)
                            menu.Path = newPath;
                        }
                    }
                }

                // Save changes
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Rollback migration - add /admin prefix back to paths
        /// </summary>
        public async Task RollbackMenuPathsAsync()
        {
            try
            {
                // Get specific paths that should have /admin prefix
                var pathsToRollback = new[]
                {
                    "/users", "/roles", "/permissions", "/menu", "/settings",
                    "/students", "/lecturers", "/departments", "/partners", "/business",
                    "/academic-years", "/semesters", "/thesis", "/thesis-periods",
                    "/internship", "/internship-periods"
                };

                var menusToRollback = await _context.Menus
                    .Where(m => pathsToRollback.Contains(m.Path))
                    .ToListAsync();

                if (!menusToRollback.Any())
                {
                    return;
                }

                foreach (var menu in menusToRollback)
                {
                    var oldPath = menu.Path;
                    var newPath = "/admin" + oldPath;
                    menu.Path = newPath;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Check if migration is needed
        /// </summary>
        public async Task<bool> IsMigrationNeededAsync()
        {
            var menusWithAdminPrefix = await _context.Menus
                .Where(m => m.Path.StartsWith("/admin/"))
                .CountAsync();

            return menusWithAdminPrefix > 0;
        }

        /// <summary>
        /// Get migration status report
        /// </summary>
        public async Task<MenuMigrationReport> GetMigrationReportAsync()
        {
            var totalMenus = await _context.Menus.CountAsync();
            var menusWithAdminPrefix = await _context.Menus
                .Where(m => m.Path.StartsWith("/admin/"))
                .CountAsync();
            var menusWithoutPrefix = totalMenus - menusWithAdminPrefix;

            return new MenuMigrationReport
            {
                TotalMenus = totalMenus,
                MenusWithAdminPrefix = menusWithAdminPrefix,
                MenusWithoutPrefix = menusWithoutPrefix,
                MigrationNeeded = menusWithAdminPrefix > 0
            };
        }
    }

    /// <summary>
    /// Report class for migration status
    /// </summary>
    public class MenuMigrationReport
    {
        public int TotalMenus { get; set; }
        public int MenusWithAdminPrefix { get; set; }
        public int MenusWithoutPrefix { get; set; }
        public bool MigrationNeeded { get; set; }
    }
} 