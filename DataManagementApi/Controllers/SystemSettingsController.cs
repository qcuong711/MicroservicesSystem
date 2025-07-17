using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.SystemSettings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/system-settings")]
    [ApiController]
    public class SystemSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SystemSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/system-settings
        [HttpGet]
        public async Task<ActionResult<object>> GetSystemSettings(
            [FromQuery] int page = 1, 
            [FromQuery] int limit = 10, 
            [FromQuery] string search = "")
        {
            try
            {
                var query = _context.SystemSettings
                    .Where(s => s.DeletedAt == null)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(s => s.SettingKey.Contains(search) || s.SettingValue.Contains(search));
                }

                var totalCount = await query.CountAsync();
                var settings = await query
                    .OrderBy(s => s.SettingKey)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(s => new SystemSettingsReadDto
                    {
                        Id = s.Id,
                        SettingKey = s.SettingKey,
                        SettingValue = s.SettingValue,
                        Description = s.Description,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt,
                        DeletedAt = s.DeletedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = settings,
                    total = totalCount,
                    page = page,
                    limit = limit
                });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // GET: api/system-settings/{key}
        [HttpGet("{key}")]
        public async Task<ActionResult<SystemSettingsReadDto>> GetSystemSetting(string key)
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key && s.DeletedAt == null);

                if (setting == null)
                {
                    return NotFound();
                }

                var dto = new SystemSettingsReadDto
                {
                    Id = setting.Id,
                    SettingKey = setting.SettingKey,
                    SettingValue = setting.SettingValue,
                    Description = setting.Description,
                    CreatedAt = setting.CreatedAt,
                    UpdatedAt = setting.UpdatedAt,
                    DeletedAt = setting.DeletedAt
                };

                return Ok(dto);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // POST: api/system-settings
        [HttpPost]
        public async Task<ActionResult<SystemSettingsReadDto>> CreateSystemSetting(SystemSettingsCreateDto dto)
        {
            try
            {
                // Kiểm tra xem key đã tồn tại chưa
                var existingSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == dto.SettingKey && s.DeletedAt == null);

                if (existingSetting != null)
                {
                    return Conflict("Setting key đã tồn tại");
                }

                var setting = new SystemSettings
                {
                    SettingKey = dto.SettingKey,
                    SettingValue = dto.SettingValue,
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _context.SystemSettings.Add(setting);
                await _context.SaveChangesAsync();

                var readDto = new SystemSettingsReadDto
                {
                    Id = setting.Id,
                    SettingKey = setting.SettingKey,
                    SettingValue = setting.SettingValue,
                    Description = setting.Description,
                    CreatedAt = setting.CreatedAt,
                    UpdatedAt = setting.UpdatedAt,
                    DeletedAt = setting.DeletedAt
                };

                return CreatedAtAction(nameof(GetSystemSetting), new { key = setting.SettingKey }, readDto);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi tạo setting");
            }
        }

        // PUT: api/system-settings/{key}
        [HttpPut("{key}")]
        public async Task<IActionResult> UpdateSystemSetting(string key, SystemSettingsUpdateDto dto)
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key && s.DeletedAt == null);

                if (setting == null)
                {
                    return NotFound();
                }

                setting.SettingValue = dto.SettingValue;
                setting.Description = dto.Description;
                setting.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi cập nhật setting");
            }
        }

        // DELETE: api/system-settings/{key}
        [HttpDelete("{key}")]
        public async Task<IActionResult> DeleteSystemSetting(string key)
        {
            try
            {
                var setting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == key && s.DeletedAt == null);

                if (setting == null)
                {
                    return NotFound();
                }

                setting.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi xóa setting");
            }
        }
    }
} 