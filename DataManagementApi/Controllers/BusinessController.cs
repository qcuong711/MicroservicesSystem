using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Business;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/business")]
    [ApiController]
    public class BusinessController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public BusinessController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/business-
        [HttpGet]
        public async Task<ActionResult<object>> GetBusiness([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.Business.Where(bf => bf.DeletedAt == null);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(bf => bf.Name.Contains(search));
            }
            var total = await query.CountAsync();
            var business = await query.OrderBy(bf => bf.DisplayOrder).ThenBy(bf => bf.Name)
                .Skip((page - 1) * limit).Take(limit)
                .Select(bf => new BusinessReadDto
                {
                    Id = bf.Id,
                    Name = bf.Name,
                    Description = bf.Description,
                    DisplayOrder = bf.DisplayOrder,
                    CreatedAt = bf.CreatedAt,
                    UpdatedAt = bf.UpdatedAt,
                    DeletedAt = bf.DeletedAt
                }).ToListAsync();
            return Ok(new { data = business, total, page, limit });
        }

        // GET: api/business-/all
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<BusinessReadDto>>> GetAllBusiness()
        {
            var business = await _context.Business.Where(bf => bf.DeletedAt == null)
                .OrderBy(bf => bf.DisplayOrder).ThenBy(bf => bf.Name)
                .Select(bf => new BusinessReadDto
                {
                    Id = bf.Id,
                    Name = bf.Name,
                    Description = bf.Description,
                    DisplayOrder = bf.DisplayOrder,
                    CreatedAt = bf.CreatedAt,
                    UpdatedAt = bf.UpdatedAt,
                    DeletedAt = bf.DeletedAt
                }).ToListAsync();
            return Ok();
        }

        // GET: api/business-/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BusinessReadDto>> GetBusiness(int id)
        {
            var bf = await _context.Business.FirstOrDefaultAsync(b => b.Id == id && b.DeletedAt == null);
            if (bf == null) return NotFound();
            var dto = new BusinessReadDto
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

        // POST: api/business-
        [HttpPost]
        public async Task<ActionResult<BusinessReadDto>> PostBusiness(BusinessCreateDto dto)
        {
            var bf = new Business
            {
                Name = dto.Name,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder
            };
            _context.Business.Add(bf);
            await _context.SaveChangesAsync();
            var result = new BusinessReadDto
            {
                Id = bf.Id,
                Name = bf.Name,
                Description = bf.Description,
                DisplayOrder = bf.DisplayOrder,
                CreatedAt = bf.CreatedAt,
                UpdatedAt = bf.UpdatedAt,
                DeletedAt = bf.DeletedAt
            };
            return CreatedAtAction(nameof(GetBusiness), new { id = bf.Id }, result);
        }

        // PUT: api/business-/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBusiness(int id, BusinessUpdateDto dto)
        {
            var bf = await _context.Business.FindAsync(id);
            if (bf == null || bf.DeletedAt != null) return NotFound();
            bf.Name = dto.Name;
            bf.Description = dto.Description;
            bf.DisplayOrder = dto.DisplayOrder;
            bf.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // SOFT DELETE: api/business-/soft-delete/{id}
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeleteBusiness(int id)
        {
            var bf = await _context.Business.FindAsync(id);
            if (bf == null) return NotFound();
            if (bf.DeletedAt != null) return BadRequest("Đã bị xóa mềm.");
            bf.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // BULK SOFT DELETE: api/business-/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách id không hợp lệ.");
            var bfs = await _context.Business.Where(b => ids.Contains(b.Id) && b.DeletedAt == null).ToListAsync();
            if (bfs.Count == 0) return NotFound("Không tìm thấy lĩnh vực nào để xóa mềm.");
            foreach (var bf in bfs) bf.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { softDeleted = bfs.Count });
        }

        // PERMANENT DELETE: api/business-/permanent-delete/{id}
        [HttpDelete("permanent-delete/{id}")]
        public async Task<IActionResult> PermanentDeleteBusiness(int id)
        {
            var bf = await _context.Business.FindAsync(id);
            if (bf == null) return NotFound();
            _context.Business.Remove(bf);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // BULK PERMANENT DELETE: api/business-/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách id không hợp lệ.");
            var bfs = await _context.Business.Where(b => ids.Contains(b.Id)).ToListAsync();
            if (bfs.Count == 0) return NotFound("Không tìm thấy lĩnh vực nào để xóa vĩnh viễn.");
            _context.Business.RemoveRange(bfs);
            await _context.SaveChangesAsync();
            return Ok(new { permanentlyDeleted = bfs.Count });
        }

        // BULK RESTORE: api/business-/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách id không hợp lệ.");
            var bfs = await _context.Business.Where(b => ids.Contains(b.Id) && b.DeletedAt != null).ToListAsync();
            if (bfs.Count == 0) return NotFound("Không tìm thấy lĩnh vực nào để khôi phục.");
            foreach (var bf in bfs) bf.DeletedAt = null;
            await _context.SaveChangesAsync();
            return Ok(new { restored = bfs.Count });
        }

        // GET: api/business-/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedBusiness([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.Business.Where(bf => bf.DeletedAt != null);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(bf => bf.Name.Contains(search));
            }
            var total = await query.CountAsync();
            var business = await query.OrderByDescending(bf => bf.DeletedAt)
                .Skip((page - 1) * limit).Take(limit)
                .Select(bf => new BusinessReadDto
                {
                    Id = bf.Id,
                    Name = bf.Name,
                    Description = bf.Description,
                    DisplayOrder = bf.DisplayOrder,
                    CreatedAt = bf.CreatedAt,
                    UpdatedAt = bf.UpdatedAt,
                    DeletedAt = bf.DeletedAt
                }).ToListAsync();
            return Ok(new { data = business, total, page, limit });
        }
    }
}
