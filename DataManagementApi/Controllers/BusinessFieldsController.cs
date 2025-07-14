using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.BusinessField;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/business-fields")]
    [ApiController]
    public class BusinessFieldsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public BusinessFieldsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/business-fields
        [HttpGet]
        public async Task<ActionResult<object>> GetBusinessFields([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.BusinessFields.Where(bf => bf.DeletedAt == null);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(bf => bf.Name.Contains(search));
            }
            var total = await query.CountAsync();
            var fields = await query.OrderBy(bf => bf.DisplayOrder).ThenBy(bf => bf.Name)
                .Skip((page - 1) * limit).Take(limit)
                .Select(bf => new BusinessFieldReadDto
                {
                    Id = bf.Id,
                    Name = bf.Name,
                    Description = bf.Description,
                    DisplayOrder = bf.DisplayOrder,
                    CreatedAt = bf.CreatedAt,
                    UpdatedAt = bf.UpdatedAt,
                    DeletedAt = bf.DeletedAt
                }).ToListAsync();
            return Ok(new { data = fields, total, page, limit });
        }

        // GET: api/business-fields/all
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<BusinessFieldReadDto>>> GetAllBusinessFields()
        {
            var fields = await _context.BusinessFields.Where(bf => bf.DeletedAt == null)
                .OrderBy(bf => bf.DisplayOrder).ThenBy(bf => bf.Name)
                .Select(bf => new BusinessFieldReadDto
                {
                    Id = bf.Id,
                    Name = bf.Name,
                    Description = bf.Description,
                    DisplayOrder = bf.DisplayOrder,
                    CreatedAt = bf.CreatedAt,
                    UpdatedAt = bf.UpdatedAt,
                    DeletedAt = bf.DeletedAt
                }).ToListAsync();
            return Ok(fields);
        }

        // GET: api/business-fields/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BusinessFieldReadDto>> GetBusinessField(int id)
        {
            var bf = await _context.BusinessFields.FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);
            if (bf == null) return NotFound();
            var dto = new BusinessFieldReadDto
            {
                Id = bf.Id,
                Name = bf.Name,
                Description = bf.Description,
                DisplayOrder = bf.DisplayOrder,
                CreatedAt = bf.CreatedAt,
                UpdatedAt = bf.UpdatedAt,
                DeletedAt = bf.DeletedAt
            };
            return Ok(dto);
        }

        // POST: api/business-fields
        [HttpPost]
        public async Task<ActionResult<BusinessFieldReadDto>> PostBusinessField(BusinessFieldCreateDto dto)
        {
            var bf = new BusinessField
            {
                Name = dto.Name,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder
            };
            _context.BusinessFields.Add(bf);
            await _context.SaveChangesAsync();
            var result = new BusinessFieldReadDto
            {
                Id = bf.Id,
                Name = bf.Name,
                Description = bf.Description,
                DisplayOrder = bf.DisplayOrder,
                CreatedAt = bf.CreatedAt,
                UpdatedAt = bf.UpdatedAt,
                DeletedAt = bf.DeletedAt
            };
            return CreatedAtAction(nameof(GetBusinessField), new { id = bf.Id }, result);
        }

        // PUT: api/business-fields/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBusinessField(int id, BusinessFieldUpdateDto dto)
        {
            var bf = await _context.BusinessFields.FindAsync(id);
            if (bf == null || bf.DeletedAt != null) return NotFound();
            bf.Name = dto.Name;
            bf.Description = dto.Description;
            bf.DisplayOrder = dto.DisplayOrder;
            bf.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // SOFT DELETE: api/business-fields/soft-delete/{id}
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteBusinessField(int id)
        {
            var bf = await _context.BusinessFields.FindAsync(id);
            if (bf == null) return NotFound();
            if (bf.DeletedAt != null) return BadRequest("Đã bị xóa mềm.");
            bf.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // BULK SOFT DELETE: api/business-fields/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách id không hợp lệ.");
            var bfs = await _context.BusinessFields.Where(b => ids.Contains(b.Id) && b.DeletedAt == null).ToListAsync();
            if (bfs.Count == 0) return NotFound("Không tìm thấy lĩnh vực nào để xóa mềm.");
            foreach (var bf in bfs) bf.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { softDeleted = bfs.Count });
        }

        // PERMANENT DELETE: api/business-fields/permanent-delete/{id}
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteBusinessField(int id)
        {
            var bf = await _context.BusinessFields.FindAsync(id);
            if (bf == null) return NotFound();
            _context.BusinessFields.Remove(bf);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // BULK PERMANENT DELETE: api/business-fields/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách id không hợp lệ.");
            var bfs = await _context.BusinessFields.Where(b => ids.Contains(b.Id)).ToListAsync();
            if (bfs.Count == 0) return NotFound("Không tìm thấy lĩnh vực nào để xóa vĩnh viễn.");
            _context.BusinessFields.RemoveRange(bfs);
            await _context.SaveChangesAsync();
            return Ok(new { permanentlyDeleted = bfs.Count });
        }

        // BULK RESTORE: api/business-fields/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách id không hợp lệ.");
            var bfs = await _context.BusinessFields.Where(b => ids.Contains(b.Id) && b.DeletedAt != null).ToListAsync();
            if (bfs.Count == 0) return NotFound("Không tìm thấy lĩnh vực nào để khôi phục.");
            foreach (var bf in bfs) bf.DeletedAt = null;
            await _context.SaveChangesAsync();
            return Ok(new { restored = bfs.Count });
        }

        // GET: api/business-fields/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedBusinessFields([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.BusinessFields.Where(bf => bf.DeletedAt != null);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(bf => bf.Name.Contains(search));
            }
            var total = await query.CountAsync();
            var fields = await query.OrderByDescending(bf => bf.DeletedAt)
                .Skip((page - 1) * limit).Take(limit)
                .Select(bf => new BusinessFieldReadDto
                {
                    Id = bf.Id,
                    Name = bf.Name,
                    Description = bf.Description,
                    DisplayOrder = bf.DisplayOrder,
                    CreatedAt = bf.CreatedAt,
                    UpdatedAt = bf.UpdatedAt,
                    DeletedAt = bf.DeletedAt
                }).ToListAsync();
            return Ok(new { data = fields, total, page, limit });
        }
    }
}
