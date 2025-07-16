using DataManagementApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SelectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private const int MaxItems = 100;

        public SelectionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("academic-years")]
        public async Task<IActionResult> GetAcademicYears([FromQuery] string? search)
        {
            var query = _context.AcademicYears.AsQueryable().Where(ay => ay.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(ay => ay.Name.ToLower().Contains(lowerSearch));
            }

            var years = await query
                .Select(ay => new { ay.Id, ay.Name })
                .Take(MaxItems)
                .ToListAsync();
            return Ok(years);
        }

        [HttpGet("semesters")]
        public async Task<IActionResult> GetSemesters([FromQuery] string? search)
        {
            var query = _context.Semesters.AsQueryable().Where(s => s.DeletedAt == null);
            
            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(lowerSearch));
            }

            var semesters = await query
                .Select(s => new { s.Id, s.Name })
                .Take(MaxItems)
                .ToListAsync();
            return Ok(semesters);
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents([FromQuery] string? search)
        {
            var query = _context.Students.AsQueryable().Where(s => s.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s => s.FullName.ToLower().Contains(lowerSearch) || s.StudentCode.ToLower().Contains(lowerSearch));
            }
            
            var students = await query
                .Select(s => new { s.Id, Name = s.FullName }) // Use FullName for consistency
                .Take(MaxItems)
                .ToListAsync();
            return Ok(students);
        }

        [HttpGet("lecturers")]
        public async Task<IActionResult> GetLecturers([FromQuery] string? search)
        {
            var query = _context.Lecturers.AsQueryable().Where(l => l.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(l => l.Name.ToLower().Contains(lowerSearch) || (l.Email != null && l.Email.ToLower().Contains(lowerSearch)));
            }

            var lecturers = await query
                .Select(l => new { l.Id, l.Name })
                .Take(MaxItems)
                .ToListAsync();
            return Ok(lecturers);
        }

        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartmentOptions([FromQuery] string? search)
        {
            var query = _context.Departments.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()) || d.Code.ToLower().Contains(search.ToLower()));
            }

            var departments = await query
                .Select(d => new { d.Id, Name = $"{d.Name} ({d.Code})" })
                .Take(100)
                .ToListAsync();

            return Ok(departments);
        }

        [HttpGet("partners")]
        public async Task<IActionResult> GetPartnerOptions([FromQuery] string? search)
        {
            var query = _context.Partners.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()));
            }

            var partners = await query
                .Select(p => new { p.Id, p.Name })
                .Take(100)
                .ToListAsync();

            return Ok(partners);
        }

        [HttpGet("menus")]
        public async Task<IActionResult> GetMenuOptions([FromQuery] string? search)
        {
            var query = _context.Menus.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => m.Name.ToLower().Contains(search.ToLower()));
            }

            var menus = await query
                .Select(m => new { Id = m.Id, Name = m.Name })
                .Take(100)
                .ToListAsync();

            return Ok(menus);
        }

        [HttpGet("permissions")]
        public async Task<IActionResult> GetPermissionOptions([FromQuery] string? search)
        {
            var query = _context.Permissions
                .Where(p => p.DeletedAt == null)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lowerSearch) || p.Module.ToLower().Contains(lowerSearch));
            }

            var permissions = await query.ToListAsync();

            var groupedPermissions = permissions
                .GroupBy(p => p.Module)
                .Select(g => new
                {
                    ModuleName = g.Key,
                    Permissions = g.Select(p => new { p.Id, p.Name, Description = p.Module + "." + p.Name }).ToList()
                })
                .OrderBy(g => g.ModuleName)
                .ToList();

            return Ok(groupedPermissions);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoleOptions([FromQuery] string? search)
        {
            var query = _context.Roles.AsQueryable().Where(r => r.DeletedAt == null);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Name.ToLower().Contains(search.ToLower()));
            }

            var roles = await query
                .Select(r => new { r.Id, r.Name })
                .Take(100)
                .ToListAsync();

            return Ok(roles);
        }
    }
} 