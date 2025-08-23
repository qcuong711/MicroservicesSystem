using DataManagementApi.Models.Dtos.CourseClass;
using DataManagementApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataManagementApi.Controllers
{
    [ApiController]
    [Route("api/courseclass")]
    public class CourseClassController : ControllerBase
    {
        private readonly ICourseClassService _courseClassService;

        public CourseClassController(ICourseClassService courseClassService)
        {
            _courseClassService = courseClassService;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10,
            [FromQuery] string search = "",
            [FromQuery] int? departmentId = null,
            [FromQuery] int? semesterId = null,
            [FromQuery] int? academicYearId = null,
            [FromQuery] int? advisorLecturerId = null)
        {
            try
            {
                var (classes, totalCount) = await _courseClassService.GetAllAsync(page, limit, search, departmentId, semesterId, academicYearId, advisorLecturerId);
                return Ok(new
                {
                    data = classes,
                    total = totalCount,
                    page = page,
                    limit = limit
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách lớp học phần", error = ex.Message });
            }
        }

        [HttpGet("deleted")]
        public async Task<ActionResult<object>> GetDeleted([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
        {
            try
            {
                var (classes, totalCount) = await _courseClassService.GetDeletedAsync(page, limit, search);
                return Ok(new
                {
                    data = classes,
                    total = totalCount,
                    page = page,
                    limit = limit
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách lớp học phần đã xóa", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CourseClassReadDto>> GetById(int id)
        {
            try
            {
                var classDto = await _courseClassService.GetByIdAsync(id);
                if (classDto == null)
                {
                    return NotFound(new { message = "Không tìm thấy lớp học phần" });
                }
                return Ok(classDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin lớp học phần", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<CourseClassReadDto>> Create([FromBody] CourseClassCreateDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var classDto = await _courseClassService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = classDto.Id }, classDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo lớp học phần", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CourseClassReadDto>> Update(int id, [FromBody] CourseClassUpdateDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var classDto = await _courseClassService.UpdateAsync(id, updateDto);
                if (classDto == null)
                {
                    return NotFound(new { message = "Không tìm thấy lớp học phần để cập nhật" });
                }

                return Ok(classDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật lớp học phần", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var result = await _courseClassService.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy lớp học phần để xóa" });
                }

                return Ok(new { message = "Xóa lớp học phần thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa lớp học phần", error = ex.Message });
            }
        }

        // RESTORE
        [HttpPatch("{id}/restore")]
        public async Task<ActionResult> Restore(int id)
        {
            try
            {
                var result = await _courseClassService.RestoreAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy lớp học phần để khôi phục" });
                }
                return Ok(new { message = "Khôi phục lớp học phần thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi khôi phục lớp học phần", error = ex.Message });
            }
        }

        // PERMANENT DELETE
        [HttpDelete("{id}/permanent")]
        public async Task<ActionResult> PermanentDelete(int id)
        {
            try
            {
                var result = await _courseClassService.PermanentDeleteAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy lớp học phần để xóa vĩnh viễn" });
                }
                return Ok(new { message = "Xóa vĩnh viễn lớp học phần thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa vĩnh viễn lớp học phần", error = ex.Message });
            }
        }

        // BULK SOFT DELETE
        [HttpPost("bulk-soft-delete")]
        public async Task<IActionResult> BulkSoftDelete([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { message = "Danh sách id không hợp lệ" });
            try
            {
                var count = await _courseClassService.BulkSoftDeleteAsync(ids);
                return Ok(new { softDeleted = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa mềm nhiều lớp học phần", error = ex.Message });
            }
        }

        // BULK RESTORE
        [HttpPost("bulk-restore")]
        public async Task<IActionResult> BulkRestore([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { message = "Danh sách id không hợp lệ" });
            try
            {
                var count = await _courseClassService.BulkRestoreAsync(ids);
                return Ok(new { restored = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi khôi phục nhiều lớp học phần", error = ex.Message });
            }
        }

        // BULK PERMANENT DELETE
        [HttpPost("bulk-permanent-delete")]
        public async Task<IActionResult> BulkPermanentDelete([FromBody] List<int> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { message = "Danh sách id không hợp lệ" });
            try
            {
                var count = await _courseClassService.BulkPermanentDeleteAsync(ids);
                return Ok(new { permanentlyDeleted = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa vĩnh viễn nhiều lớp học phần", error = ex.Message });
            }
        }
    }
}