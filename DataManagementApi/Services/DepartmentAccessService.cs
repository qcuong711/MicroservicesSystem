using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DataManagementApi.Services
{
    public class DepartmentAccessService
    {
        private readonly ApplicationDbContext _context;

        public DepartmentAccessService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách ID phòng ban mà user có thể truy cập
        /// </summary>
        public async Task<List<int>> GetAccessibleDepartmentIds(ClaimsPrincipal userClaims)
        {
            // Sử dụng extension method để lấy Keycloak User ID
            var keycloakUserId = userClaims.GetKeycloakUserId();
            
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return new List<int>();
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null);

            if (user == null)
            {
                return new List<int>();
            }

            // Kiểm tra xem user có phải admin không
            var isAdmin = user.UserRoles.Any(ur => ur.Role.Name == "ADMIN");
            if (isAdmin)
            {
                // Admin xem tất cả phòng ban
                return await _context.Departments
                    .Where(d => d.DeletedAt == null)
                    .Select(d => d.Id)
                    .ToListAsync();
            }

            // Lấy cấu hình hệ thống
            var allowCrossAccess = await GetSystemSettingValue("ALLOW_CROSS_DEPARTMENT_DATA_ACCESS");
            
            if (allowCrossAccess == "true")
            {
                // Cho phép xem tất cả phòng ban
                return await _context.Departments
                    .Where(d => d.DeletedAt == null)
                    .Select(d => d.Id)
                    .ToListAsync();
            }
            else
            {
                // Chỉ xem phòng ban mình + phòng ban con
                // Giả sử user có DepartmentId (cần thêm field này vào User model)
                // Hiện tại sẽ trả về tất cả vì User model chưa có DepartmentId
                return await _context.Departments
                    .Where(d => d.DeletedAt == null)
                    .Select(d => d.Id)
                    .ToListAsync();
            }
        }

        /// <summary>
        /// Lấy phòng ban của user và tất cả phòng ban con
        /// </summary>
        public async Task<List<int>> GetDepartmentAndChildren(int departmentId)
        {
            var result = new List<int> { departmentId };

            // Lấy tất cả phòng ban con (đệ quy)
            var children = await _context.Departments
                .Where(d => d.ParentDepartmentId == departmentId && d.DeletedAt == null)
                .Select(d => d.Id)
                .ToListAsync();

            result.AddRange(children);

            // Đệ quy cho các phòng ban con
            foreach (var childId in children)
            {
                var grandChildren = await GetDepartmentAndChildren(childId);
                result.AddRange(grandChildren);
            }

            return result.Distinct().ToList();
        }

        /// <summary>
        /// Lấy giá trị setting từ database
        /// </summary>
        private async Task<string> GetSystemSettingValue(string key)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key && s.DeletedAt == null);

            return setting?.SettingValue ?? "false";
        }

        /// <summary>
        /// Khởi tạo cấu hình mặc định nếu chưa có
        /// </summary>
        public async Task InitializeDefaultSettings()
        {
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