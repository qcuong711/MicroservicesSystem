using DataManagementApi.Data;
using DataManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Services
{
    /// <summary>
    /// Service để tạo admin user mặc định cho testing
    /// </summary>
    public class AdminUserSeeder
    {
        private readonly ApplicationDbContext _context;

        public AdminUserSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tạo admin user mặc định nếu chưa có
        /// </summary>
        public async Task SeedDefaultAdminAsync()
        {
            try
            {
                // Check xem đã có admin user chưa
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole == null)
                {
                    return;
                }

                var existingAdmin = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.RoleId == adminRole.Id));

                if (existingAdmin != null)
                {
                    return;
                }

                // Tạo admin user mặc định
                var adminUser = new User
                {
                    KeycloakUserId = "admin-default-001", // Temporary ID
                    Name = "System Administrator",
                    Email = "admin@example.com",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Users.AddAsync(adminUser);
                await _context.SaveChangesAsync();

                // Assign Admin role
                var userRole = new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                };

                await _context.UserRoles.AddAsync(userRole);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Error creating admin user
            }
        }

        /// <summary>
        /// Cập nhật user hiện tại thành admin (dựa trên email hoặc keycloak ID)
        /// </summary>
        public async Task PromoteUserToAdminAsync(string emailOrKeycloakId)
        {
            try
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole == null)
                {
                    return;
                }

                // Tìm user theo email hoặc KeycloakUserId
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Email == emailOrKeycloakId || u.KeycloakUserId == emailOrKeycloakId);

                if (user == null)
                {
                    return;
                }

                // Check xem user đã có role Admin chưa
                var hasAdminRole = user.UserRoles.Any(ur => ur.RoleId == adminRole.Id);
                if (hasAdminRole)
                {
                    return;
                }

                // Add Admin role
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = adminRole.Id
                };

                await _context.UserRoles.AddAsync(userRole);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Error promoting user to admin
            }
        }

        /// <summary>
        /// List tất cả users và roles của họ (for debugging)
        /// </summary>
        public async Task ListAllUsersWithRolesAsync()
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.DeletedAt == null)
                    .ToListAsync();

                // List all users and roles for debugging
            }
            catch (Exception ex)
            {
                // Error listing users
            }
        }
    }
} 