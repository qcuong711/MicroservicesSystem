using DataManagementApi.Data;
using DataManagementApi.Models;
using DataManagementApi.Extensions;  
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Json;

namespace DataManagementApi.Services
{
    /// <summary>
    /// Service để log và quản lý permission audit trail
    /// </summary>
    public class PermissionAuditService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PermissionAuditService> _logger;

        public PermissionAuditService(
            IServiceProvider serviceProvider,
            ILogger<PermissionAuditService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Log permission check activity
        /// </summary>
        public async Task LogPermissionCheckAsync(
            ClaimsPrincipal user,
            string moduleName,
            string action,
            bool granted,
            HttpContext httpContext,
            string? resource = null,
            string? deniedReason = null,
            object? additionalData = null)
        {
            try
            {
                // Create a new DbContext instance for this operation to avoid concurrency issues
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var keycloakUserId = user.GetKeycloakUserId() ?? "anonymous";
                var userName = user.FindFirst(ClaimTypes.Name)?.Value;
                var userEmail = user.FindFirst(ClaimTypes.Email)?.Value;

                var auditLog = new PermissionAuditLog
                {
                    UserId = keycloakUserId,
                    UserName = userName,
                    UserEmail = userEmail,
                    ModuleName = moduleName,
                    Action = action,
                    Resource = resource,
                    PermissionGranted = granted,
                    DeniedReason = deniedReason,
                    IpAddress = GetClientIpAddress(httpContext),
                    UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
                    RequestPath = httpContext.Request.Path.Value,
                    HttpMethod = httpContext.Request.Method,
                    Timestamp = DateTime.UtcNow,
                    AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null,
                    Severity = GetAuditSeverity(granted, deniedReason)
                };

                context.PermissionAuditLogs.Add(auditLog);
                await context.SaveChangesAsync();

                // Log to console for immediate visibility
                var logLevel = granted ? LogLevel.Information : LogLevel.Warning;
                _logger.Log(logLevel, 
                    "Permission {Status}: User {UserId} {Action} on {Module} - {Resource}",
                    granted ? "GRANTED" : "DENIED",
                    keycloakUserId,
                    action,
                    moduleName,
                    resource ?? "N/A");
            }
            catch (Exception ex)
            {
                // Don't let audit logging fail the main operation
                _logger.LogError(ex, "Failed to log permission audit");
            }
        }

        /// <summary>
        /// Get audit logs with filtering and pagination
        /// </summary>
        public async Task<(List<PermissionAuditLog> logs, int total)> GetAuditLogsAsync(
            int page = 1,
            int limit = 50,
            string? userId = null,
            string? moduleName = null,
            string? action = null,
            bool? granted = null,
            AuditSeverity? severity = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var query = context.PermissionAuditLogs.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(log => log.UserId.Contains(userId));
            }

            if (!string.IsNullOrEmpty(moduleName))
            {
                query = query.Where(log => log.ModuleName == moduleName);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(log => log.Action == action);
            }

            if (granted.HasValue)
            {
                query = query.Where(log => log.PermissionGranted == granted.Value);
            }

            if (severity.HasValue)
            {
                query = query.Where(log => log.Severity == severity.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(log => log.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(log => log.Timestamp <= toDate.Value);
            }

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(log => log.Timestamp)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return (logs, total);
        }

        /// <summary>
        /// Get audit statistics
        /// </summary>
        public async Task<object> GetAuditStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var query = context.PermissionAuditLogs.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(log => log.Timestamp >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(log => log.Timestamp <= toDate.Value);

            var stats = new
            {
                TotalRequests = await query.CountAsync(),
                GrantedRequests = await query.CountAsync(log => log.PermissionGranted),
                DeniedRequests = await query.CountAsync(log => !log.PermissionGranted),
                
                TopModules = await query
                    .GroupBy(log => log.ModuleName)
                    .Select(g => new { Module = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync(),

                TopActions = await query
                    .GroupBy(log => log.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync(),

                SeverityBreakdown = await query
                    .GroupBy(log => log.Severity)
                    .Select(g => new { Severity = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync(),

                RecentActivity = await query
                    .OrderByDescending(log => log.Timestamp)
                    .Take(20)
                    .Select(log => new
                    {
                        log.Timestamp,
                        log.UserId,
                        log.UserName,
                        log.ModuleName,
                        log.Action,
                        log.PermissionGranted,
                        log.Severity
                    })
                    .ToListAsync()
            };

            return stats;
        }

        /// <summary>
        /// Clean up old audit logs (for performance)
        /// </summary>
        public async Task CleanupOldLogsAsync(int retentionDays = 90)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                
                var oldLogs = await context.PermissionAuditLogs
                    .Where(log => log.Timestamp < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    context.PermissionAuditLogs.RemoveRange(oldLogs);
                    await context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Cleaned up {oldLogs.Count} audit logs older than {retentionDays} days");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old audit logs");
            }
        }

        /// <summary>
        /// Extract client IP address from HttpContext
        /// </summary>
        private static string GetClientIpAddress(HttpContext context)
        {
            // Try to get real IP behind proxy
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',').FirstOrDefault()?.Trim() ?? "unknown";
            }

            var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// Determine audit severity based on permission result
        /// </summary>
        private static AuditSeverity GetAuditSeverity(bool granted, string? deniedReason)
        {
            if (granted)
            {
                return AuditSeverity.Info;
            }

            // Check for potential security concerns
            if (!string.IsNullOrEmpty(deniedReason))
            {
                var reason = deniedReason.ToLower();
                if (reason.Contains("suspicious") || reason.Contains("violation") || reason.Contains("attack"))
                {
                    return AuditSeverity.Critical;
                }
                if (reason.Contains("error") || reason.Contains("exception"))
                {
                    return AuditSeverity.Error;
                }
            }

            return AuditSeverity.Warning; // Default for denied access
        }
    }
} 