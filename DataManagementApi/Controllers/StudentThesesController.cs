using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Models.Dtos.Thesis;
using DataManagementApi.Services;
using DataManagementApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Claims;

namespace DataManagementApi.Controllers
{
    [Route("api/student/theses")]
    [ApiController]
    [Authorize]
    public class StudentThesesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;

        public StudentThesesController(ApplicationDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        // GET: api/student/theses
        [HttpGet]
        public async Task<ActionResult<object>> GetMyTheses(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10
        )
        {
            try
            {
                // Lấy thông tin sinh viên hiện tại từ token
                var currentStudent = await GetCurrentStudentAsync();
                if (currentStudent == null)
                {
                    return Unauthorized("Bạn không phải là sinh viên hoặc không tìm thấy thông tin sinh viên.");
                }

                var query = _context.Theses
                    .Where(t => t.StudentId == currentStudent.Id && t.DeletedAt == null)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .Include(t => t.AcademicYear)
                    .Include(t => t.Semester)
                    .AsQueryable();

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
                        StudentName = currentStudent.FullName,
                        StudentCode = currentStudent.StudentCode,
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
                        UpdatedAt = t.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new { data = theses, total });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi lấy danh sách khóa luận: {ex.Message}");
            }
        }

        // GET: api/student/theses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ThesisReadDto>> GetThesis(int id)
        {
            try
            {
                // Lấy thông tin sinh viên hiện tại từ token
                var currentStudent = await GetCurrentStudentAsync();
                if (currentStudent == null)
                {
                    return Unauthorized("Bạn không phải là sinh viên hoặc không tìm thấy thông tin sinh viên.");
                }

                var thesis = await _context.Theses
                    .Where(t => t.Id == id && t.StudentId == currentStudent.Id && t.DeletedAt == null)
                    .Include(t => t.Supervisor)
                    .Include(t => t.Examiner)
                    .Include(t => t.ThesisPeriod)
                    .Include(t => t.AcademicYear)
                    .Include(t => t.Semester)
                    .FirstOrDefaultAsync();

                if (thesis == null)
                {
                    return NotFound("Không tìm thấy khóa luận hoặc bạn không có quyền truy cập.");
                }

                var dto = new ThesisReadDto
                {
                    Id = thesis.Id,
                    Title = thesis.Title,
                    Description = thesis.Description,
                    StudentId = thesis.StudentId,
                    StudentName = currentStudent.FullName,
                    StudentCode = currentStudent.StudentCode,
                    SupervisorId = thesis.SupervisorId,
                    SupervisorName = thesis.Supervisor != null ? thesis.Supervisor.Name : null,
                    SupervisorEmail = thesis.Supervisor != null ? thesis.Supervisor.Email : null,
                    ExaminerId = thesis.ExaminerId,
                    ExaminerName = thesis.Examiner != null ? thesis.Examiner.Name : null,
                    ExaminerEmail = thesis.Examiner != null ? thesis.Examiner.Email : null,
                    ThesisPeriodId = thesis.ThesisPeriodId,
                    ThesisPeriodName = thesis.ThesisPeriod != null ? thesis.ThesisPeriod.Name : null,
                    AcademicYearId = thesis.AcademicYearId,
                    AcademicYearName = thesis.AcademicYear != null ? thesis.AcademicYear.Name : null,
                    SemesterId = thesis.SemesterId,
                    SemesterName = thesis.Semester != null ? thesis.Semester.Name : null,
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
                    UpdatedAt = thesis.UpdatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi lấy thông tin khóa luận: {ex.Message}");
            }
        }

        // POST: api/student/theses
        [HttpPost]
        public async Task<ActionResult<ThesisReadDto>> SubmitThesis([FromForm] StudentThesisSubmitDto thesisDto)
        {
            try
            {
                // Lấy thông tin sinh viên hiện tại từ token
                var currentStudent = await GetCurrentStudentAsync();
                if (currentStudent == null)
                {
                    return Unauthorized("Bạn không phải là sinh viên hoặc không tìm thấy thông tin sinh viên.");
                }

                // Kiểm tra xem đợt khóa luận có tồn tại không
                var thesisPeriod = await _context.ThesisPeriods.FindAsync(thesisDto.ThesisPeriodId);
                if (thesisPeriod == null)
                {
                    return BadRequest("Đợt khóa luận không tồn tại.");
                }

                // Kiểm tra xem năm học có tồn tại không
                var academicYear = await _context.AcademicYears.FindAsync(thesisDto.AcademicYearId);
                if (academicYear == null)
                {
                    return BadRequest("Năm học không tồn tại.");
                }

                // Kiểm tra xem học kỳ có tồn tại không
                var semester = await _context.Semesters.FindAsync(thesisDto.SemesterId);
                if (semester == null)
                {
                    return BadRequest("Học kỳ không tồn tại.");
                }

                // Kiểm tra xem sinh viên đã có khóa luận trong đợt này chưa
                var existingThesis = await _context.Theses
                    .Where(t => t.StudentId == currentStudent.Id && 
                           t.ThesisPeriodId == thesisDto.ThesisPeriodId && 
                           t.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (existingThesis != null)
                {
                    return BadRequest("Bạn đã có khóa luận trong đợt này. Vui lòng cập nhật khóa luận hiện có thay vì tạo mới.");
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
                
                // Tạo mới khóa luận
                var thesis = new Thesis
                {
                    Title = thesisDto.Title,
                    Description = thesisDto.Description,
                    StudentId = currentStudent.Id,
                    // Giảng viên hướng dẫn sẽ được gán sau bởi giảng viên/quản trị viên
                    ThesisPeriodId = thesisDto.ThesisPeriodId,
                    AcademicYearId = thesisDto.AcademicYearId,
                    SemesterId = thesisDto.SemesterId,
                    SubmissionDate = DateTime.UtcNow,
                    Status = "Draft", // Trạng thái mặc định là nháp
                    ReportUrl = reportUrl,
                    FileName = fileName,
                    FileType = fileType,
                    FileSize = fileSize,
                    UploadDate = uploadDate,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Theses.Add(thesis);
                await _context.SaveChangesAsync();

                // Tải lại thesis để lấy đầy đủ thông tin liên quan
                var createdThesis = await _context.Theses
                    .Where(t => t.Id == thesis.Id)
                    .Include(t => t.ThesisPeriod)
                    .Include(t => t.AcademicYear)
                    .Include(t => t.Semester)
                    .Select(t => new ThesisReadDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Description = t.Description,
                        StudentId = t.StudentId,
                        StudentName = currentStudent.FullName,
                        StudentCode = currentStudent.StudentCode,
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
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
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

        // PUT: api/student/theses/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateThesis(int id, [FromForm] StudentThesisSubmitDto thesisDto)
        {
            try
            {
                // Lấy thông tin sinh viên hiện tại từ token
                var currentStudent = await GetCurrentStudentAsync();
                if (currentStudent == null)
                {
                    return Unauthorized("Bạn không phải là sinh viên hoặc không tìm thấy thông tin sinh viên.");
                }

                // Tìm khóa luận cần cập nhật
                var existingThesis = await _context.Theses
                    .Where(t => t.Id == id && t.StudentId == currentStudent.Id && t.DeletedAt == null)
                    .FirstOrDefaultAsync();

                if (existingThesis == null)
                {
                    return NotFound("Không tìm thấy khóa luận hoặc bạn không có quyền cập nhật.");
                }

                // Kiểm tra nếu trạng thái là Approved thì không cho phép cập nhật
                if (existingThesis.Status == "Approved")
                {
                    return BadRequest("Không thể cập nhật khóa luận đã được phê duyệt.");
                }

                // Kiểm tra xem đợt khóa luận có tồn tại không
                var thesisPeriod = await _context.ThesisPeriods.FindAsync(thesisDto.ThesisPeriodId);
                if (thesisPeriod == null)
                {
                    return BadRequest("Đợt khóa luận không tồn tại.");
                }

                // Kiểm tra xem năm học có tồn tại không
                var academicYear = await _context.AcademicYears.FindAsync(thesisDto.AcademicYearId);
                if (academicYear == null)
                {
                    return BadRequest("Năm học không tồn tại.");
                }

                // Kiểm tra xem học kỳ có tồn tại không
                var semester = await _context.Semesters.FindAsync(thesisDto.SemesterId);
                if (semester == null)
                {
                    return BadRequest("Học kỳ không tồn tại.");
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

                // Cập nhật thông tin khóa luận
                existingThesis.Title = thesisDto.Title;
                existingThesis.Description = thesisDto.Description;
                existingThesis.ThesisPeriodId = thesisDto.ThesisPeriodId;
                existingThesis.AcademicYearId = thesisDto.AcademicYearId;
                existingThesis.SemesterId = thesisDto.SemesterId;
                existingThesis.SubmissionDate = DateTime.UtcNow;
                existingThesis.Status = "Draft"; // Cập nhật lại trạng thái là nháp
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
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi cập nhật khóa luận: {ex.Message}");
            }
        }

        // GET: api/student/theses/periods
        [HttpGet("periods")]
        public async Task<ActionResult<object>> GetThesisPeriods()
        {
            try
            {
                // Lấy danh sách các đợt khóa luận hiện tại (chưa kết thúc)
                var currentDate = DateTime.UtcNow;
                var periods = await _context.ThesisPeriods
                    .Where(p => p.EndDate >= currentDate)
                    .Include(p => p.AcademicYear)
                    .Include(p => p.Semester)
                    .OrderBy(p => p.StartDate)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        AcademicYearId = p.AcademicYearId,
                        AcademicYearName = p.AcademicYear != null ? p.AcademicYear.Name : null,
                        SemesterId = p.SemesterId,
                        SemesterName = p.Semester != null ? p.Semester.Name : null
                    })
                    .ToListAsync();

                return Ok(periods);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi lấy danh sách đợt khóa luận: {ex.Message}");
            }
        }

        // GET: api/student/theses/academic-years
        [HttpGet("academic-years")]
        public async Task<ActionResult<object>> GetAcademicYears()
        {
            try
            {
                var academicYears = await _context.AcademicYears
                    .Where(a => a.DeletedAt == null)
                    .OrderByDescending(a => a.Name)
                    .Select(a => new
                    {
                        Id = a.Id,
                        Name = a.Name
                    })
                    .ToListAsync();

                return Ok(academicYears);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi lấy danh sách năm học: {ex.Message}");
            }
        }

        // GET: api/student/theses/semesters
        [HttpGet("semesters")]
        public async Task<ActionResult<object>> GetSemesters()
        {
            try
            {
                var semesters = await _context.Semesters
                    .Where(s => s.DeletedAt == null)
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        Id = s.Id,
                        Name = s.Name
                    })
                    .ToListAsync();

                return Ok(semesters);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Lỗi khi lấy danh sách học kỳ: {ex.Message}");
            }
        }

        // Helper method to get current student from token
        private async Task<Student?> GetCurrentStudentAsync()
        {
            // Lấy Keycloak User ID từ JWT token
            var keycloakUserId = User.GetKeycloakUserId();
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                return null;
            }

            // Tìm user trong database theo KeycloakUserId
            var user = await _context.Users
                .Where(u => u.KeycloakUserId == keycloakUserId && u.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return null;
            }

            // Tìm sinh viên theo email
            var student = await _context.Students
                .Where(s => s.Email == user.Email && s.DeletedAt == null)
                .FirstOrDefaultAsync();

            return student;
        }

        private bool ThesisExists(int id)
        {
            return _context.Theses.Any(e => e.Id == id && e.DeletedAt == null);
        }
    }
}