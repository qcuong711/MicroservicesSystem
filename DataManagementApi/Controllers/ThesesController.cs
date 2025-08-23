using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Thesis;
using DataManagementApi.Services;
using DataManagementApi.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Claims;

namespace DataManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThesesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly DepartmentAccessService _departmentAccessService;
        private readonly IFileService _fileService;

        public ThesesController(ApplicationDbContext context, DepartmentAccessService departmentAccessService, IFileService fileService)
        {
            _context = context;
            _departmentAccessService = departmentAccessService;
            _fileService = fileService;
        }


        // GET: api/Theses
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<object>> GetTheses(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string search = "",
            [FromQuery] DateTime? submissionDate = null
        )
        {
            try
            {
                // Lấy danh sách phòng ban có thể truy cập
                var accessibleDepartmentIds = await _departmentAccessService.GetAccessibleDepartmentIds(User);
                
                var query = _context.Theses
                    .Where(t => t.DeletedAt == null)
                    .Include(t => t.Student)
                    .ThenInclude(s => s.Department)
                    .Include(t => t.Supervisor)
                    .ThenInclude(s => s.Department)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .Where(t => t.Student == null || t.Supervisor == null || 
                               (t.Student != null && (!t.Student.DepartmentId.HasValue || accessibleDepartmentIds.Contains(t.Student.DepartmentId.Value))) ||
                               (t.Supervisor != null && (!t.Supervisor.DepartmentId.HasValue || accessibleDepartmentIds.Contains(t.Supervisor.DepartmentId.Value))))
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(t =>
                        t.Title.Contains(search) ||
                        (t.Student != null && t.Student.FullName != null && t.Student.FullName.Contains(search)) ||
                        (t.Supervisor != null && t.Supervisor.Name != null && t.Supervisor.Name.Contains(search))
                    );
                }

                if (submissionDate.HasValue)
                {
                    var date = submissionDate.Value.Date;
                    query = query.Where(t => t.SubmissionDate.Date == date);
                }

                var total = await query.CountAsync();
                var theses = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(t => new ThesisReadDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        StudentId = t.StudentId,
                        StudentName = t.Student != null ? t.Student.FullName : null,
                        StudentCode = t.Student != null ? t.Student.StudentCode : null,
                        SupervisorId = t.SupervisorId,
                        SupervisorName = t.Supervisor != null ? t.Supervisor.Name : null,
                        SupervisorEmail = t.Supervisor != null ? t.Supervisor.Email : null,
                        ExaminerId = t.ExaminerId,
                        ExaminerName = t.Examiner != null ? t.Examiner.Name : null,
                        ExaminerEmail = t.Examiner != null ? t.Examiner.Email : null,
                        ThesisPeriodId = t.ThesisPeriodId,
                        ThesisPeriodName = t.ThesisPeriod != null ? t.ThesisPeriod.Name : null,
                        AcademicYearId = t.AcademicYearId,
                        SemesterId = t.SemesterId,
                        SubmissionDate = t.SubmissionDate,
                        Status = t.Status,
                        // File information
                        ReportUrl = t.ReportUrl,
                        FileName = t.FileName,
                        FileType = t.FileType,
                        FileSize = t.FileSize,
                        UploadDate = t.UploadDate,
                        // Score information
                        Score = t.Score,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        DeletedAt = t.DeletedAt
                    })
                    .ToListAsync();

                return Ok(new { data = theses, total });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // GET: api/Theses/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ThesisReadDto>> GetThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses
                    .Where(t => t.Id == id && t.DeletedAt == null)
                    .Include(t => t.Student)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .FirstOrDefaultAsync();

                if (thesis == null)
                {
                    return NotFound();
                }

                var dto = new ThesisReadDto
                {
                    Id = thesis.Id,
                    Title = thesis.Title,
                    Description = thesis.Description,
                    StudentId = thesis.StudentId,
                    StudentName = thesis.Student != null ? thesis.Student.FullName : null,
                    SupervisorId = thesis.SupervisorId,
                    SupervisorName = thesis.Supervisor != null ? thesis.Supervisor.Name : null,
                    SupervisorEmail = thesis.Supervisor != null ? thesis.Supervisor.Email : null,
                    ExaminerId = thesis.ExaminerId,
                    ExaminerName = thesis.Examiner != null ? thesis.Examiner.Name : null,
                    ExaminerEmail = thesis.Examiner != null ? thesis.Examiner.Email : null,
                    ThesisPeriodId = thesis.ThesisPeriodId,
                    ThesisPeriodName = thesis.ThesisPeriod != null ? thesis.ThesisPeriod.Name : null,
                    AcademicYearId = thesis.AcademicYearId,
                    SemesterId = thesis.SemesterId,
                    SubmissionDate = thesis.SubmissionDate,
                    Status = thesis.Status,
                    // File information
                    ReportUrl = thesis.ReportUrl,
                    FileName = thesis.FileName,
                    FileType = thesis.FileType,
                    FileSize = thesis.FileSize,
                    UploadDate = thesis.UploadDate,
                    // Score information
                    Score = thesis.Score,
                    CreatedAt = thesis.CreatedAt,
                    UpdatedAt = thesis.UpdatedAt,
                    DeletedAt = thesis.DeletedAt
                };

                return Ok(dto);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi truy xuất dữ liệu từ cơ sở dữ liệu");
            }
        }

        // PUT: api/Theses/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutThesis(int id, [FromForm] ThesisUpdateDto thesisDto)
        {
            try
            {
                var existingThesis = await _context.Theses.FindAsync(id);
                if (existingThesis == null)
                {
                    return NotFound();
                }
                
                // Kiểm tra nếu trạng thái là Approved và người dùng là sinh viên thì không cho phép cập nhật
                if (existingThesis.Status == "Approved")
                {
                    // Kiểm tra xem người dùng hiện tại có phải là sinh viên không
                    // Nếu là sinh viên, không cho phép cập nhật
                    var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.KeycloakUserId == currentUserId);
                    
                    if (currentUser != null)
                    {
                        // Đơn giản hóa logic kiểm tra - nếu là sinh viên, không cho phép cập nhật
                        // Trong thực tế, cần có cơ chế xác định vai trò người dùng
                        var isStudent = await _context.Students.AnyAsync(s => s.Email == currentUser.Email);
                        if (isStudent)
                        {
                            return BadRequest("Không thể cập nhật luận văn đã được phê duyệt");
                        }
                    }
                }

                var student = await _context.Students.FindAsync(thesisDto.StudentId);
                if (student == null)
                {
                    return BadRequest("Sinh viên không tồn tại");
                }

                var supervisor = await _context.Lecturers.FindAsync(thesisDto.SupervisorId);
                if (supervisor == null)
                {
                    return BadRequest("Giảng viên hướng dẫn không tồn tại");
                }

                Lecturer? examiner = null;
                if (thesisDto.ExaminerId.HasValue)
                {
                    examiner = await _context.Lecturers.FindAsync(thesisDto.ExaminerId.Value);
                    if (examiner == null)
                    {
                        return BadRequest("Giảng viên phản biện không tồn tại");
                    }
                }

                var thesisPeriod = await _context.ThesisPeriods.FindAsync(thesisDto.ThesisPeriodId);
                if (thesisPeriod == null)
                {
                    return BadRequest("Đợt khóa luận không tồn tại");
                }
                
                // Xử lý file upload nếu có
                if (thesisDto.ThesisFile != null && thesisDto.ThesisFile.Length > 0)
                {
                    try
                    {
                        // Xóa file cũ nếu có
                        if (!string.IsNullOrEmpty(existingThesis.ReportUrl) && !string.IsNullOrEmpty(existingThesis.FileName))
                        {
                            var oldFilePath = Path.Combine(_fileService.GetRootPath(), "uploads/theses", existingThesis.FileName);
                            _fileService.DeleteFile(oldFilePath);
                        }
                        
                        // Lưu file mới
                        var fileResult = await _fileService.SaveFileAsync(thesisDto.ThesisFile, "uploads/theses");
                        existingThesis.FileName = fileResult.fileName;
                        existingThesis.ReportUrl = _fileService.GetFileUrl(fileResult.fileName, "uploads/theses");
                        existingThesis.FileType = fileResult.fileType;
                        existingThesis.FileSize = fileResult.fileSize;
                        existingThesis.UploadDate = DateTime.UtcNow;
                    }
                    catch (ArgumentException ex)
                    {
                        return BadRequest(ex.Message);
                    }
                }
                else if (thesisDto.ExistingFileName != null)
                {
                    // Giữ lại thông tin file cũ nếu không có file mới
                    existingThesis.FileName = thesisDto.ExistingFileName;
                    existingThesis.ReportUrl = thesisDto.ExistingReportUrl;
                    existingThesis.FileType = thesisDto.ExistingFileType;
                    existingThesis.FileSize = thesisDto.ExistingFileSize;
                }

                existingThesis.Title = thesisDto.Title;
                existingThesis.Description = thesisDto.Description;
                existingThesis.StudentId = thesisDto.StudentId;
                existingThesis.SupervisorId = thesisDto.SupervisorId;
                existingThesis.ExaminerId = thesisDto.ExaminerId;
                existingThesis.ThesisPeriodId = thesisDto.ThesisPeriodId;
                existingThesis.AcademicYearId = thesisDto.AcademicYearId;
                existingThesis.SemesterId = thesisDto.SemesterId;
                existingThesis.SubmissionDate = thesisDto.SubmissionDate;
                existingThesis.Status = thesisDto.Status;
                
                // Cập nhật điểm số nếu có
                if (thesisDto.Score.HasValue)
                {
                    existingThesis.Score = thesisDto.Score;
                    // Nếu có điểm số, tự động cập nhật trạng thái thành Approved
                    existingThesis.Status = "Approved";
                }
                
                existingThesis.UpdatedAt = DateTime.UtcNow;

                _context.Entry(existingThesis).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ThesisExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi cập nhật dữ liệu");
            }
        }

        // POST: api/Theses
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ThesisReadDto>> PostThesis([FromForm] ThesisCreateDto thesisDto)
        {
            try
            {
                var student = await _context.Students.FindAsync(thesisDto.StudentId);
                if (student == null)
                {
                    return BadRequest("Sinh viên không tồn tại");
                }

                var supervisor = await _context.Lecturers.FindAsync(thesisDto.SupervisorId);
                if (supervisor == null)
                {
                    return BadRequest("Giảng viên hướng dẫn không tồn tại");
                }

                Lecturer? examiner = null;
                if (thesisDto.ExaminerId.HasValue)
                {
                    examiner = await _context.Lecturers.FindAsync(thesisDto.ExaminerId.Value);
                    if (examiner == null)
                    {
                        return BadRequest("Giảng viên phản biện không tồn tại");
                    }
                }

                var thesisPeriod = await _context.ThesisPeriods.FindAsync(thesisDto.ThesisPeriodId);
                if (thesisPeriod == null)
                {
                    return BadRequest("Đợt khóa luận không tồn tại");
                }

                // Xử lý file upload nếu có
                string? reportUrl = null;
                string? fileName = null;
                string? fileType = null;
                long? fileSize = null;
                DateTime? uploadDate = null;
                
                if (thesisDto.ThesisFile != null && thesisDto.ThesisFile.Length > 0)
                {
                    try
                    {
                        var fileResult = await _fileService.SaveFileAsync(thesisDto.ThesisFile, "uploads/theses");
                        fileName = fileResult.fileName;
                        reportUrl = _fileService.GetFileUrl(fileResult.fileName, "uploads/theses");
                        fileType = fileResult.fileType;
                        fileSize = fileResult.fileSize;
                        uploadDate = DateTime.UtcNow;
                    }
                    catch (ArgumentException ex)
                    {
                        return BadRequest(ex.Message);
                    }
                }
                
                var thesis = new Thesis
                {
                    Title = thesisDto.Title,
                    Description = thesisDto.Description,
                    StudentId = thesisDto.StudentId,
                    SupervisorId = thesisDto.SupervisorId,
                    ExaminerId = thesisDto.ExaminerId,
                    ThesisPeriodId = thesisDto.ThesisPeriodId,
                    AcademicYearId = thesisDto.AcademicYearId,
                    SemesterId = thesisDto.SemesterId,
                    SubmissionDate = thesisDto.SubmissionDate,
                    Status = thesisDto.Status ?? "Draft",
                    ReportUrl = reportUrl,
                    FileName = fileName,
                    FileType = fileType,
                    FileSize = fileSize,
                    UploadDate = uploadDate,
                    Score = thesisDto.Score,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Theses.Add(thesis);
                await _context.SaveChangesAsync();

                // Tải lại thesis để lấy đầy đủ thông tin liên quan
                var createdThesis = await _context.Theses
                    .Where(t => t.Id == thesis.Id)
                    .Include(t => t.Student)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .Include(t => t.AcademicYear)
                    .Include(t => t.Semester)
                    .Select(t => new ThesisReadDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        StudentId = t.StudentId,
                        StudentName = t.Student != null ? t.Student.FullName : null,
                        StudentCode = t.Student != null ? t.Student.StudentCode : null,
                        SupervisorId = t.SupervisorId,
                        SupervisorName = t.Supervisor != null ? t.Supervisor.Name : null,
                        SupervisorEmail = t.Supervisor != null ? t.Supervisor.Email : null,
                        ExaminerId = t.ExaminerId,
                        ExaminerName = t.Examiner != null ? t.Examiner.Name : null,
                        ExaminerEmail = t.Examiner != null ? t.Examiner.Email : null,
                        ThesisPeriodId = t.ThesisPeriodId,
                        ThesisPeriodName = t.ThesisPeriod != null ? t.ThesisPeriod.Name : null,
                        AcademicYearId = t.AcademicYearId,
                        AcademicYearName = t.AcademicYear != null ? t.AcademicYear.Name : null,
                        SemesterId = t.SemesterId,
                        SemesterName = t.Semester != null ? t.Semester.Name : null,
                        SubmissionDate = t.SubmissionDate,
                        Status = t.Status,
                        // File information
                        ReportUrl = t.ReportUrl,
                        FileName = t.FileName,
                        FileType = t.FileType,
                        FileSize = t.FileSize,
                        UploadDate = t.UploadDate,
                        // Score information
                        Score = t.Score,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        DeletedAt = t.DeletedAt
                    })
                    .FirstOrDefaultAsync();

                if (createdThesis == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi tạo mới khóa luận");
                }

                return CreatedAtAction(nameof(GetThesis), new { id = thesis.Id }, createdThesis);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi tạo mới khóa luận: {ex.Message}");
            }
        }

        // SOFT DELETE: api/Theses/soft-delete/5
        [HttpPost("soft-delete/{id}")]
        [Authorize]
        public async Task<IActionResult> SoftDeleteThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses.FindAsync(id);
                if (thesis == null)
                {
                    return NotFound();
                }
                thesis.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa mềm khóa luận");
            }
        }

        // PERMANENT DELETE: api/Theses/permanent-delete/5
        [HttpDelete("permanent-delete/{id}")]
        [Authorize]
        public async Task<IActionResult> PermanentDeleteThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
                if (thesis == null)
                {
                    return NotFound();
                }
                _context.Theses.Remove(thesis);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa vĩnh viễn khóa luận");
            }
        }

        // RESTORE: api/Theses/restore/5
        [HttpPost("restore/{id}")]
        [Authorize]
        public async Task<IActionResult> RestoreThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
                if (thesis == null)
                {
                    return NotFound();
                }
                thesis.DeletedAt = null;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi khôi phục khóa luận");
            }
        }

        // GET DELETED: api/Theses/deleted
        [HttpGet("deleted")]
        [Authorize]
        public async Task<ActionResult<object>> GetDeletedTheses([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Theses
                    .Where(t => t.DeletedAt != null)
                    .Include(t => t.Student)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                     query = query.Where(t =>
                        t.Title.Contains(search) ||
                        (t.Student != null && t.Student.FullName.Contains(search))
                    );
                }
                var total = await query.CountAsync();
                var theses = await query
                    .OrderByDescending(t => t.DeletedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(t => new ThesisReadDto {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        StudentId = t.StudentId,
                        StudentName = t.Student != null ? t.Student.FullName : null,
                        SupervisorId = t.SupervisorId,
                        SupervisorName = t.Supervisor != null ? t.Supervisor.Name : null,
                        ExaminerId = t.ExaminerId,
                        ExaminerName = t.Examiner != null ? t.Examiner.Name : null,
                        ThesisPeriodId = t.ThesisPeriodId,
                        ThesisPeriodName = t.ThesisPeriod != null ? t.ThesisPeriod.Name : null,
                        SubmissionDate = t.SubmissionDate,
                        Status = t.Status,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        DeletedAt = t.DeletedAt
                    })
                    .ToListAsync();
                return Ok(new { data = theses, total });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi lấy danh sách khóa luận đã xóa");
            }
        }

        // BULK SOFT DELETE
        [HttpPost("bulk-soft-delete")]
        [Authorize]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            try
            {
                var theses = await _context.Theses.Where(t => ids.Contains(t.Id)).ToListAsync();
                if (theses.Count != ids.Count)
                {
                    var notFoundIds = ids.Except(theses.Select(t => t.Id)).ToList();
                    return NotFound($"Không tìm thấy các khóa luận với ID: {string.Join(", ", notFoundIds)}");
                }
                foreach (var thesis in theses)
                {
                    thesis.DeletedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return Ok(new { softDeleted = theses.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xóa mềm nhiều khóa luận: {ex.Message}\n{ex.StackTrace}");
            }
        }

        // BULK RESTORE
        [HttpPost("bulk-restore")]
        [Authorize]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            try
            {
                var theses = await _context.Theses.IgnoreQueryFilters().Where(t => ids.Contains(t.Id)).ToListAsync();
                foreach (var thesis in theses)
                {
                    thesis.DeletedAt = null;
                }
                await _context.SaveChangesAsync();
                return Ok(new { restored = theses.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi khôi phục nhiều khóa luận: {ex.Message}");
            }
        }
        
        // BULK PERMANENT DELETE
        [HttpPost("bulk-permanent-delete")]
        [Authorize]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any()) return BadRequest("Danh sách ID không hợp lệ.");
            try
            {
                var theses = await _context.Theses.IgnoreQueryFilters().Where(t => ids.Contains(t.Id)).ToListAsync();
                _context.Theses.RemoveRange(theses);
                await _context.SaveChangesAsync();
                return Ok(new { permanentlyDeleted = theses.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xóa vĩnh viễn nhiều khóa luận: {ex.Message}");
            }
        }

        // DELETE: api/Theses/5 (This is now replaced by soft-delete and permanent-delete)
        // I'll comment it out to avoid confusion, but it could be removed entirely.
        /*
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteThesis(int id)
        {
            try
            {
                var thesis = await _context.Theses.FindAsync(id);
                if (thesis == null)
                {
                    return NotFound();
                }

                _context.Theses.Remove(thesis);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Lỗi khi xóa khóa luận");
            }
        }
        */

        private bool ThesisExists(int id)
        {
            return _context.Theses.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}