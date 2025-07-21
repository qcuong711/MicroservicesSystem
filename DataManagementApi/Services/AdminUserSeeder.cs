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
                    Console.WriteLine("AdminUserSeeder: Admin role not found, skipping admin user creation");
                    return;
                }

                var existingAdmin = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.UserRoles.Any(ur => ur.RoleId == adminRole.Id));

                if (existingAdmin != null)
                {
                    Console.WriteLine($"AdminUserSeeder: Admin user already exists - {existingAdmin.Email}");
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

                Console.WriteLine($"AdminUserSeeder: Created default admin user - {adminUser.Email} with role Admin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminUserSeeder: Error creating admin user - {ex.Message}");
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
                    Console.WriteLine("AdminUserSeeder: Admin role not found");
                    return;
                }

                // Tìm user theo email hoặc KeycloakUserId
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .FirstOrDefaultAsync(u => u.Email == emailOrKeycloakId || u.KeycloakUserId == emailOrKeycloakId);

                if (user == null)
                {
                    Console.WriteLine($"AdminUserSeeder: User not found - {emailOrKeycloakId}");
                    return;
                }

                // Check xem user đã có role Admin chưa
                var hasAdminRole = user.UserRoles.Any(ur => ur.RoleId == adminRole.Id);
                if (hasAdminRole)
                {
                    Console.WriteLine($"AdminUserSeeder: User {user.Email} already has Admin role");
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

                Console.WriteLine($"AdminUserSeeder: Promoted user {user.Email} to Admin role");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminUserSeeder: Error promoting user to admin - {ex.Message}");
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

                Console.WriteLine("=== ALL USERS AND THEIR ROLES ===");
                foreach (var user in users)
                {
                    var roles = string.Join(", ", user.UserRoles.Select(ur => ur.Role.Name));
                    Console.WriteLine($"User: {user.Email} ({user.KeycloakUserId}) - Roles: [{roles}]");
                }
                Console.WriteLine("=== END USER LIST ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AdminUserSeeder: Error listing users - {ex.Message}");
            }
        }
    }
} 