using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Partner;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PartnersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PartnersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Partners
        [HttpGet]
        public async Task<ActionResult<object>> GetPartners([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.Partners.Where(p => p.DeletedAt == null);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Email.Contains(search) || p.PhoneNumber.Contains(search));
            }
            var total = await query.CountAsync();
            var partners = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => new PartnerReadDto {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Address = p.Address,
                    Website = p.Website,
                    PhoneNumber = p.PhoneNumber,
                    ContactPerson = p.ContactPerson,
                    Email = p.Email,
                    IsActive = p.IsActive,
                    DeletedAt = p.DeletedAt,
                    CreatedAt = DateTime.UtcNow,
                    // Các trường mở rộng nếu có
                })
                .ToListAsync();
            return Ok(new { data = partners, total, page, limit });
        }

        // GET: api/Partners/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PartnerReadDto>> GetPartner(int id)
        {
            var p = await _context.Partners.FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            if (p == null) return NotFound();
            var dto = new PartnerReadDto {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Address = p.Address,
                Website = p.Website,
                PhoneNumber = p.PhoneNumber,
                ContactPerson = p.ContactPerson,
                Email = p.Email,
                IsActive = p.IsActive,
                DeletedAt = p.DeletedAt,
                CreatedAt = DateTime.UtcNow,
            };
            return Ok(dto);
        }

        // GET: api/Partners/deleted
        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeletedPartners([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            var query = _context.Partners.Where(p => p.DeletedAt != null);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Email.Contains(search) || p.PhoneNumber.Contains(search));
            }

            var total = await query.CountAsync();
            var partners = await query
                .OrderByDescending(p => p.DeletedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return Ok(new { data = partners, total, page, limit });
        }

        // PUT: api/Partners/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPartner(int id, PartnerUpdateDto partnerDto)
        {
            var existingPartner = await _context.Partners.FirstOrDefaultAsync(p => p.Id == id);
            if (existingPartner == null || existingPartner.DeletedAt != null)
            {
                return NotFound("Đối tác không tồn tại hoặc đã bị xóa.");
            }
            existingPartner.Name = partnerDto.Name;
            existingPartner.Email = partnerDto.Email;
            existingPartner.PhoneNumber = partnerDto.PhoneNumber;
            existingPartner.Address = partnerDto.Address ?? string.Empty;
            existingPartner.Website = partnerDto.Website;
            existingPartner.Description = partnerDto.Description;
            existingPartner.ContactPerson = partnerDto.ContactPerson;
            existingPartner.IsActive = partnerDto.IsActive;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PartnerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            var dto = new PartnerReadDto {
                Id = existingPartner.Id,
                Name = existingPartner.Name,
                Description = existingPartner.Description,
                Address = existingPartner.Address,
                Website = existingPartner.Website,
                PhoneNumber = existingPartner.PhoneNumber,
                ContactPerson = existingPartner.ContactPerson,
                Email = existingPartner.Email,
                IsActive = existingPartner.IsActive,
                DeletedAt = existingPartner.DeletedAt,
                CreatedAt = DateTime.UtcNow,
            };
            return Ok(dto);
        }

        // POST: api/Partners
        [HttpPost]
        public async Task<ActionResult<PartnerReadDto>> PostPartner(PartnerCreateDto partnerDto)
        {
            var partner = new Partner {
                Name = partnerDto.Name,
                Description = partnerDto.Description,
                Address = partnerDto.Address,
                Website = partnerDto.Website,
                PhoneNumber = partnerDto.PhoneNumber,
                ContactPerson = partnerDto.ContactPerson,
                Email = partnerDto.Email,
                IsActive = partnerDto.IsActive,
                DeletedAt = null
            };
            _context.Partners.Add(partner);
            await _context.SaveChangesAsync();
            var dto = new PartnerReadDto {
                Id = partner.Id,
                Name = partner.Name,
                Description = partner.Description,
                Address = partner.Address,
                Website = partner.Website,
                PhoneNumber = partner.PhoneNumber,
                ContactPerson = partner.ContactPerson,
                Email = partner.Email,
                IsActive = partner.IsActive,
                DeletedAt = partner.DeletedAt,
                CreatedAt = DateTime.UtcNow,
            };
            return CreatedAtAction(nameof(GetPartner), new { id = partner.Id }, dto);
        }

        // SOFT DELETE: api/Partners/soft-delete/5
        [HttpPost("soft-delete/{id}")]
        public async Task<IActionResult> SoftDeletePartner(int id)
        {
            var partner = await _context.Partners.FindAsync(id);
            if (partner == null) return NotFound();
            if (partner.DeletedAt != null) return BadRequest("Đối tác đã được xóa.");

            partner.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // BULK SOFT DELETE: api/Partners/bulk-soft-delete
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            var partners = await _context.Partners.Where(p => ids.Contains(p.Id) && p.DeletedAt == null).ToListAsync();
            if (!partners.Any()) return NotFound("Không tìm thấy đối tác hợp lệ để xóa.");
            
            foreach (var partner in partners)
            {
                partner.DeletedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa thành công {partners.Count} đối tác." });
        }

        // BULK RESTORE: api/Partners/bulk-restore
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            var partners = await _context.Partners.Where(p => ids.Contains(p.Id) && p.DeletedAt != null).ToListAsync();
            if (!partners.Any()) return NotFound("Không tìm thấy đối tác hợp lệ để khôi phục.");

            foreach (var partner in partners)
            {
                partner.DeletedAt = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã khôi phục thành công {partners.Count} đối tác." });
        }

        // BULK PERMANENT DELETE: api/Partners/bulk-permanent-delete
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            var partners = await _context.Partners.Where(p => ids.Contains(p.Id)).ToListAsync();
            if (!partners.Any()) return NotFound("Không tìm thấy đối tác hợp lệ để xóa vĩnh viễn.");

            _context.Partners.RemoveRange(partners);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã xóa vĩnh viễn {partners.Count} đối tác." });
        }

        private bool PartnerExists(int id)
        {
            return _context.Partners.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}