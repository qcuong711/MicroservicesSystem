using DataManagementApi.Data;
using DataManagementApi.Models.Dtos.CourseClass;
using DataManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Services
{
    public interface ICourseClassService
    {
        // Paginated queries with filters
        Task<(IEnumerable<CourseClassReadDto> Classes, int TotalCount)> GetAllAsync(int page, int limit, string search, int? departmentId, int? semesterId, int? academicYearId, int? advisorLecturerId);
        Task<(IEnumerable<CourseClassReadDto> Classes, int TotalCount)> GetDeletedAsync(int page, int limit, string search);
        // Single item operations
        Task<CourseClassReadDto?> GetByIdAsync(int id);
        Task<CourseClassReadDto> CreateAsync(CourseClassCreateDto createDto);
        Task<CourseClassReadDto?> UpdateAsync(int id, CourseClassUpdateDto updateDto);
        Task<bool> DeleteAsync(int id);
        Task<bool> RestoreAsync(int id);
        Task<bool> PermanentDeleteAsync(int id);
        // Bulk operations
        Task<int> BulkSoftDeleteAsync(List<int> ids);
        Task<int> BulkRestoreAsync(List<int> ids);
        Task<int> BulkPermanentDeleteAsync(List<int> ids);
    }

    public class CourseClassService : ICourseClassService
    {
        private readonly ApplicationDbContext _context;

        public CourseClassService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<CourseClassReadDto> Classes, int TotalCount)> GetAllAsync(int page, int limit, string search, int? departmentId, int? semesterId, int? academicYearId, int? advisorLecturerId)
        {
            var query = _context.Classes
                .AsNoTracking()
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Include(c => c.AcademicYear)
                .Include(c => c.AdvisorLecturer)
                .Where(c => c.IsActive);

            if (departmentId.HasValue)
                query = query.Where(c => c.DepartmentId == departmentId.Value);
            if (semesterId.HasValue)
                query = query.Where(c => c.SemesterId == semesterId.Value);
            if (academicYearId.HasValue)
                query = query.Where(c => c.AcademicYearId == academicYearId.Value);
            if (advisorLecturerId.HasValue)
                query = query.Where(c => c.AdvisorLecturerId == advisorLecturerId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    (c.Name != null && c.Name.Contains(search)) ||
                    (c.Department != null && c.Department.Name != null && c.Department.Name.Contains(search)) ||
                    (c.Semester != null && c.Semester.Name != null && c.Semester.Name.Contains(search)) ||
                    (c.AcademicYear != null && c.AcademicYear.Name != null && c.AcademicYear.Name.Contains(search)) ||
                    (c.AdvisorLecturer != null && c.AdvisorLecturer.Name != null && c.AdvisorLecturer.Name.Contains(search))
                );
            }

            var totalCount = await query.CountAsync();

            var classes = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new CourseClassReadDto
                {
                    Id = c.Id,
                    Name = c.Name ?? string.Empty,
                    DepartmentId = c.DepartmentId,
                    DepartmentName = c.Department != null ? c.Department.Name : null,
                    SemesterId = c.SemesterId,
                    SemesterName = c.Semester != null ? c.Semester.Name : null,
                    AcademicYearId = c.AcademicYearId,
                    AcademicYearName = c.AcademicYear != null ? c.AcademicYear.Name : null,
                    AdvisorLecturerId = c.AdvisorLecturerId,
                    AdvisorLecturerName = c.AdvisorLecturer != null ? c.AdvisorLecturer.Name : null,
                    Notes = c.Notes ?? string.Empty,
                    StudentCount = 0,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return (classes, totalCount);
        }

        public async Task<CourseClassReadDto?> GetByIdAsync(int id)
        {
            var classEntity = await _context.Classes
                .AsNoTracking()
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Include(c => c.AcademicYear)
                .Include(c => c.AdvisorLecturer)
                .Where(c => c.Id == id && c.IsActive)
                .Select(c => new
                {
                    Entity = c,
                    DepartmentName = c.Department != null ? c.Department.Name : null,
                    SemesterName = c.Semester != null ? c.Semester.Name : null,
                    AcademicYearName = c.AcademicYear != null ? c.AcademicYear.Name : null,
                    AdvisorLecturerName = c.AdvisorLecturer != null ? c.AdvisorLecturer.Name : null
                })
                .FirstOrDefaultAsync();

            if (classEntity == null)
                return null;

            return new CourseClassReadDto
            {
                Id = classEntity.Entity.Id,
                Name = classEntity.Entity.Name,
                DepartmentId = classEntity.Entity.DepartmentId,
                DepartmentName = classEntity.DepartmentName,
                SemesterId = classEntity.Entity.SemesterId,
                SemesterName = classEntity.SemesterName,
                AcademicYearId = classEntity.Entity.AcademicYearId,
                AcademicYearName = classEntity.AcademicYearName,
                AdvisorLecturerId = classEntity.Entity.AdvisorLecturerId,
                AdvisorLecturerName = classEntity.AdvisorLecturerName,
                Notes = classEntity.Entity.Notes,
                StudentCount = 0,
                IsActive = classEntity.Entity.IsActive,
                CreatedAt = classEntity.Entity.CreatedAt,
                UpdatedAt = classEntity.Entity.UpdatedAt
            };
        }

        public async Task<CourseClassReadDto> CreateAsync(CourseClassCreateDto createDto)
        {
            var classEntity = new CourseClass
            {
                Name = createDto.Name,
                DepartmentId = createDto.DepartmentId,
                SemesterId = createDto.SemesterId,
                AcademicYearId = createDto.AcademicYearId,
                AdvisorLecturerId = createDto.AdvisorLecturerId,
                Notes = createDto.Notes,
                IsActive = createDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Classes.Add(classEntity);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(classEntity.Id) ?? throw new InvalidOperationException("Failed to retrieve created class");
        }

        public async Task<CourseClassReadDto?> UpdateAsync(int id, CourseClassUpdateDto updateDto)
        {
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (classEntity == null)
                return null;

            classEntity.Name = updateDto.Name;
            classEntity.DepartmentId = updateDto.DepartmentId;
            classEntity.SemesterId = updateDto.SemesterId;
            classEntity.AcademicYearId = updateDto.AcademicYearId;
            classEntity.AdvisorLecturerId = updateDto.AdvisorLecturerId;
            classEntity.Notes = updateDto.Notes;
            classEntity.IsActive = updateDto.IsActive;
            classEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (classEntity == null)
                return false;

            var now = DateTime.UtcNow;
            classEntity.IsActive = false;
            classEntity.DeletedAt = now;
            classEntity.UpdatedAt = now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsActive && c.DeletedAt != null);

            if (classEntity == null)
                return false;

            classEntity.IsActive = true;
            classEntity.DeletedAt = null;
            classEntity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PermanentDeleteAsync(int id)
        {
            var classEntity = await _context.Classes
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsActive && c.DeletedAt != null);

            if (classEntity == null)
                return false;

            _context.Classes.Remove(classEntity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<(IEnumerable<CourseClassReadDto> Classes, int TotalCount)> GetDeletedAsync(int page, int limit, string search)
        {
            var query = _context.Classes
                .AsNoTracking()
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Include(c => c.AcademicYear)
                .Include(c => c.AdvisorLecturer)
                .Where(c => !c.IsActive && c.DeletedAt != null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    (c.Name != null && c.Name.Contains(search)) ||
                    (c.Department != null && c.Department.Name != null && c.Department.Name.Contains(search)) ||
                    (c.Semester != null && c.Semester.Name != null && c.Semester.Name.Contains(search)) ||
                    (c.AcademicYear != null && c.AcademicYear.Name != null && c.AcademicYear.Name.Contains(search)) ||
                    (c.AdvisorLecturer != null && c.AdvisorLecturer.Name != null && c.AdvisorLecturer.Name.Contains(search))
                );
            }

            var totalCount = await query.CountAsync();

            var classes = await query
                .OrderByDescending(c => c.DeletedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new CourseClassReadDto
                {
                    Id = c.Id,
                    Name = c.Name ?? string.Empty,
                    DepartmentId = c.DepartmentId,
                    DepartmentName = c.Department != null ? c.Department.Name : null,
                    SemesterId = c.SemesterId,
                    SemesterName = c.Semester != null ? c.Semester.Name : null,
                    AcademicYearId = c.AcademicYearId,
                    AcademicYearName = c.AcademicYear != null ? c.AcademicYear.Name : null,
                    AdvisorLecturerId = c.AdvisorLecturerId,
                    AdvisorLecturerName = c.AdvisorLecturer != null ? c.AdvisorLecturer.Name : null,
                    Notes = c.Notes ?? string.Empty,
                    StudentCount = 0,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return (classes, totalCount);
        }

        public async Task<int> BulkSoftDeleteAsync(List<int> ids)
        {
            var classes = await _context.Classes
                .Where(c => ids.Contains(c.Id) && c.IsActive)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var classEntity in classes)
            {
                classEntity.IsActive = false;
                classEntity.DeletedAt = now;
                classEntity.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return classes.Count;
        }

        public async Task<int> BulkRestoreAsync(List<int> ids)
        {
            var classes = await _context.Classes
                .Where(c => ids.Contains(c.Id) && !c.IsActive && c.DeletedAt != null)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var classEntity in classes)
            {
                classEntity.IsActive = true;
                classEntity.DeletedAt = null;
                classEntity.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return classes.Count;
        }

        public async Task<int> BulkPermanentDeleteAsync(List<int> ids)
        {
            var classes = await _context.Classes
                .Include(c => c.Department)
                .Include(c => c.Semester)
                .Include(c => c.AcademicYear)
                .Include(c => c.AdvisorLecturer)
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();

            _context.Classes.RemoveRange(classes);
            await _context.SaveChangesAsync();
            return classes.Count;
        }
    }
}