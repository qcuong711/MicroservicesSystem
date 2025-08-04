using DataManagementApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DataManagementApi.Controllers
{
    [Route("api/modules")]
    [ApiController]
    [Authorize]
    public class ModulesController : ControllerBase
    {
        /// <summary>
        /// Get all available modules with detailed information
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<object>>> GetModules([FromQuery] string? category = null)
        {
            try
            {
                IEnumerable<ModuleDefinition> modules = ModuleRegistry.Modules.Values.Where(m => m.IsActive);
                
                if (!string.IsNullOrEmpty(category))
                {
                    modules = modules.Where(m => m.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                var result = modules
                    .OrderBy(m => m.DisplayOrder)
                    .ThenBy(m => m.Name)
                    .Select(m => new
                    {
                        m.Name,
                        m.DisplayName,
                        m.Description,
                        m.Category,
                        m.DisplayOrder,
                        m.AvailablePermissions,
                        ApiPaths = m.ApiPaths,
                        MenuPaths = m.MenuPaths
                    })
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy danh sách modules: {ex.Message}");
            }
        }

        /// <summary>
        /// Get modules grouped by category
        /// </summary>
        [HttpGet("by-category")]
        public async Task<ActionResult<object>> GetModulesByCategory()
        {
            try
            {
                var modulesByCategory = ModuleRegistry.Modules.Values
                    .Where(m => m.IsActive)
                    .GroupBy(m => m.Category)
                    .OrderBy(g => g.Key)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(m => m.DisplayOrder)
                              .Select(m => new
                              {
                                  m.Name,
                                  m.DisplayName,
                                  m.Description,
                                  m.AvailablePermissions
                              })
                              .ToList()
                    );

                return Ok(modulesByCategory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy modules theo category: {ex.Message}");
            }
        }

        /// <summary>
        /// Get module names only (for backward compatibility)
        /// </summary>
        [HttpGet("names")]
        public async Task<ActionResult<List<string>>> GetModuleNames()
        {
            try
            {
                var moduleNames = ModuleRegistry.GetModuleNames();
                return Ok(moduleNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy tên modules: {ex.Message}");
            }
        }

        /// <summary>
        /// Get detailed information about a specific module
        /// </summary>
        [HttpGet("{moduleName}")]
        public async Task<ActionResult<object>> GetModule(string moduleName)
        {
            try
            {
                var module = ModuleRegistry.GetModule(moduleName);
                if (module == null)
                {
                    return NotFound($"Module '{moduleName}' không tồn tại");
                }

                var result = new
                {
                    module.Name,
                    module.DisplayName,
                    module.Description,
                    module.Category,
                    module.DisplayOrder,
                    module.AvailablePermissions,
                    ApiPaths = module.ApiPaths,
                    MenuPaths = module.MenuPaths,
                    module.IsActive
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy thông tin module: {ex.Message}");
            }
        }

        /// <summary>
        /// Get menu paths for a specific module
        /// </summary>
        [HttpGet("{moduleName}/menu-paths")]
        public async Task<ActionResult<List<string>>> GetModuleMenuPaths(string moduleName)
        {
            try
            {
                var menuPaths = ModuleRegistry.GetMenuPathsForModule(moduleName);
                return Ok(menuPaths);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy menu paths: {ex.Message}");
            }
        }
    }
} 