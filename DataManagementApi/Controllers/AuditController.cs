using DataManagementApi.Services;
using DataManagementApi.Models;
using DataManagementApi.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DataManagementApi.Controllers
{
    [Route("api/audit")]
    [ApiController]
    [Authorize]
    public class AuditController : ControllerBase
    {
        private readonly PermissionAuditService _auditService;
        private readonly SimpleMatrixPermissionService _permissionService;

        public AuditController(
            PermissionAuditService auditService,
            SimpleMatrixPermissionService permissionService)
        {
            _auditService = auditService;
            _permissionService = permissionService;
        }

        /// <summary>
        /// Get audit logs with filtering and pagination
        /// </summary>
        [HttpGet("logs")]
        public async Task<ActionResult<object>> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50,
            [FromQuery] string? userId = null,
            [FromQuery] string? moduleName = null,
            [FromQuery] string? action = null,
            [FromQuery] bool? granted = null,
            [FromQuery] AuditSeverity? severity = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                // Check if user can read audit logs (Admin only)
                var canReadAudit = await _permissionService.IsAdminAsync(User);
                if (!canReadAudit)
                {
                    return Forbid("Chỉ Admin mới có thể xem audit logs");
                }

                var (logs, total) = await _auditService.GetAuditLogsAsync(
                    page, limit, userId, moduleName, action, granted, severity, fromDate, toDate);

                var response = new
                {
                    data = logs.Select(log => new
                    {
                        log.Id,
                        log.UserId,
                        log.UserName,
                        log.UserEmail,
                        log.ModuleName,
                        log.Action,
                        log.Resource,
                        log.PermissionGranted,
                        log.DeniedReason,
                        log.IpAddress,
                        log.RequestPath,
                        log.HttpMethod,
                        log.Timestamp,
                        Severity = log.Severity.ToString()
                    }),
                    pagination = new
                    {
                        page,
                        limit,
                        total,
                        totalPages = (int)Math.Ceiling((double)total / limit)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy audit logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Get audit statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetAuditStats(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var canReadAudit = await _permissionService.IsAdminAsync(User);
                if (!canReadAudit)
                {
                    return Forbid("Chỉ Admin mới có thể xem audit statistics");
                }

                var stats = await _auditService.GetAuditStatsAsync(fromDate, toDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy audit statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up old audit logs (Admin only)
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<ActionResult> CleanupOldLogs([FromQuery] int retentionDays = 90)
        {
            try
            {
                var isAdmin = await _permissionService.IsAdminAsync(User);
                if (!isAdmin)
                {
                    return Forbid("Chỉ Admin mới có thể cleanup audit logs");
                }

                await _auditService.CleanupOldLogsAsync(retentionDays);
                return Ok(new { message = $"Đã cleanup audit logs cũ hơn {retentionDays} ngày" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi cleanup audit logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Get my audit logs (current user only)
        /// </summary>
        [HttpGet("my-logs")]
        public async Task<ActionResult<object>> GetMyAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int limit = 50,
            [FromQuery] string? moduleName = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var keycloakUserId = User.GetKeycloakUserId();
                if (string.IsNullOrEmpty(keycloakUserId))
                {
                    return Unauthorized("Không tìm thấy thông tin user");
                }

                var (logs, total) = await _auditService.GetAuditLogsAsync(
                    page, limit, keycloakUserId, moduleName, action, null, null, fromDate, toDate);

                var response = new
                {
                    data = logs.Select(log => new
                    {
                        log.Id,
                        log.ModuleName,
                        log.Action,
                        log.Resource,
                        log.PermissionGranted,
                        log.RequestPath,
                        log.HttpMethod,
                        log.Timestamp,
                        Severity = log.Severity.ToString()
                    }),
                    pagination = new
                    {
                        page,
                        limit,
                        total,
                        totalPages = (int)Math.Ceiling((double)total / limit)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy audit logs của bạn: {ex.Message}");
            }
        }

        /// <summary>
        /// Get available filters for audit logs
        /// </summary>
        [HttpGet("filters")]
        public async Task<ActionResult<object>> GetAuditFilters()
        {
            try
            {
                var canReadAudit = await _permissionService.IsAdminAsync(User);
                if (!canReadAudit)
                {
                    return Forbid("Chỉ Admin mới có thể xem audit filters");
                }

                var filters = new
                {
                    Modules = ModuleRegistry.GetModuleNames(),
                    Actions = new[] { "Create", "Read", "Update", "Delete", "Export", "Approve", "Assign" },
                    Severities = Enum.GetNames<AuditSeverity>()
                };

                return Ok(filters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy filters: {ex.Message}");
            }
        }
    }
} 