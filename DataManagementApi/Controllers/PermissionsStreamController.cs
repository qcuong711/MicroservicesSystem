using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Security.Claims;
using DataManagementApi.Services;
using System.Collections.Concurrent;
using System.Text.Json;

namespace DataManagementApi.Controllers
{
    [Route("api/permissions")]
    [ApiController]
    public class PermissionsStreamController : ControllerBase
    {
        private readonly SimpleMatrixPermissionService _permissionService;
        private readonly ILogger<PermissionsStreamController> _logger;
        
        // Static dictionary để track active SSE connections
        private static readonly ConcurrentDictionary<string, (StreamWriter writer, string userId)> _sseConnections = new();
        private static readonly Timer _keepAliveTimer;

        static PermissionsStreamController()
        {
            // Keep-alive timer to prevent connection timeout
            _keepAliveTimer = new Timer(SendKeepAlive, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public PermissionsStreamController(
            SimpleMatrixPermissionService permissionService,
            ILogger<PermissionsStreamController> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        /// <summary>
        /// Server-Sent Events endpoint for real-time permission updates
        /// </summary>
        [HttpGet("stream")]
        public async Task StreamPermissionUpdates([FromQuery] string access_token)
        {
            // Validate access token (simplified - in production use proper JWT validation)
            if (string.IsNullOrEmpty(access_token))
            {
                Response.StatusCode = 401;
                return;
            }

            var connectionId = Guid.NewGuid().ToString();
            
            try
            {
                // Setup SSE response headers
                Response.Headers.Add("Content-Type", "text/event-stream");
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("Access-Control-Allow-Headers", "Cache-Control");

                // Flush headers immediately
                await Response.Body.FlushAsync();

                var writer = new StreamWriter(Response.Body, Encoding.UTF8, leaveOpen: true);
                
                try
                {
                    // Send initial connection message
                    await SendSseMessage(writer, "connected", new { connectionId, timestamp = DateTime.UtcNow });
                    
                    // Add connection to tracking dictionary
                    var userId = ExtractUserIdFromToken(access_token);
                    _sseConnections.TryAdd(connectionId, (writer, userId));
                    
                    _logger.LogInformation($"SSE connection established: {connectionId} for user {userId}");

                    // Keep connection alive until client disconnects
                    try
                    {
                        // Wait for cancellation (client disconnect)
                        await HttpContext.RequestAborted.WaitHandle.AsTask();
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation($"SSE connection closed: {connectionId}");
                    }
                }
                finally
                {
                    // Proper async disposal of StreamWriter
                    try
                    {
                        await writer.FlushAsync();
                        await writer.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error disposing StreamWriter");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SSE connection: {connectionId}");
            }
            finally
            {
                // Clean up connection
                _sseConnections.TryRemove(connectionId, out _);
            }
        }

        /// <summary>
        /// Broadcast permission update to specific user
        /// </summary>
        public static async Task BroadcastPermissionUpdate(string userId, object permissions)
        {
            var connectionsToNotify = _sseConnections
                .Where(kvp => kvp.Value.userId == userId)
                .ToList();

            foreach (var (connectionId, (writer, _)) in connectionsToNotify)
            {
                try
                {
                    await SendSseMessage(writer, "permission_updated", new { userId, permissions });
                }
                catch (Exception)
                {
                    // Connection is dead, remove it
                    _sseConnections.TryRemove(connectionId, out _);
                }
            }
        }

        /// <summary>
        /// Broadcast role update to specific user
        /// </summary>
        public static async Task BroadcastRoleUpdate(string userId)
        {
            var connectionsToNotify = _sseConnections
                .Where(kvp => kvp.Value.userId == userId)
                .ToList();

            foreach (var (connectionId, (writer, _)) in connectionsToNotify)
            {
                try
                {
                    await SendSseMessage(writer, "role_updated", new { userId, timestamp = DateTime.UtcNow });
                }
                catch (Exception)
                {
                    // Connection is dead, remove it
                    _sseConnections.TryRemove(connectionId, out _);
                }
            }
        }

        /// <summary>
        /// Send SSE message with proper formatting
        /// </summary>
        private static async Task SendSseMessage(StreamWriter writer, string eventType, object data)
        {
            var json = JsonSerializer.Serialize(new { type = eventType, data });
            await writer.WriteLineAsync($"data: {json}");
            await writer.WriteLineAsync(); // Empty line to separate messages
            await writer.FlushAsync();
        }

        /// <summary>
        /// Send keep-alive messages to all connections
        /// </summary>
        private static async void SendKeepAlive(object? state)
        {
            var deadConnections = new List<string>();

            foreach (var (connectionId, (writer, userId)) in _sseConnections)
            {
                try
                {
                    await SendSseMessage(writer, "keepalive", new { timestamp = DateTime.UtcNow });
                }
                catch (Exception)
                {
                    // Connection is dead, mark for removal
                    deadConnections.Add(connectionId);
                }
            }

            // Clean up dead connections
            foreach (var connectionId in deadConnections)
            {
                _sseConnections.TryRemove(connectionId, out _);
            }
        }

        /// <summary>
        /// Extract user ID from access token (simplified - use proper JWT parsing in production)
        /// </summary>
        private static string ExtractUserIdFromToken(string accessToken)
        {
            try
            {
                // This is a simplified version - in production, properly validate JWT
                // For now, we'll use a placeholder
                return "extracted_user_id"; // Replace with actual JWT parsing
            }
            catch
            {
                return "unknown_user";
            }
        }

        /// <summary>
        /// Get active connection statistics
        /// </summary>
        [HttpGet("stream/stats")]
        [Authorize]
        public async Task<ActionResult<object>> GetStreamStats()
        {
            try
            {
                var isAdmin = await _permissionService.IsAdminAsync(User);
                if (!isAdmin)
                {
                    return Forbid("Chỉ Admin mới có thể xem stream statistics");
                }

                var stats = new
                {
                    ActiveConnections = _sseConnections.Count,
                    ConnectionsByUser = _sseConnections
                        .GroupBy(kvp => kvp.Value.userId)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    Timestamp = DateTime.UtcNow
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi lấy stream stats: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Extension method to convert WaitHandle to Task
    /// </summary>
    public static class WaitHandleExtensions
    {
        public static Task AsTask(this WaitHandle handle)
        {
            return Task.Run(() => handle.WaitOne());
        }
    }
} 