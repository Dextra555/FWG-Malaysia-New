using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;

namespace OBMS.WebAPI.Controllers
{
    /// <summary>
    /// Notification Framework endpoints.
    ///
    /// Covers:
    ///   - Get notifications for a user
    ///   - Get unread count (for bell badge)
    ///   - Mark as read / mark all as read / dismiss
    ///   - Get current low-stock alerts
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationRepository _notif;

        public NotificationController(INotificationRepository notif)
        {
            _notif = notif;
        }

        // ── Retrieve ─────────────────────────────────────────────────────────

        /// <summary>Returns the latest notifications for the current user.</summary>
        [HttpGet("GetNotifications")]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] string username,
            [FromQuery] int maxRows = 50)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "Username is required." });

            try
            {
                var list = await _notif.GetNotifications(username, maxRows);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to load notifications.", detail = ex.Message });
            }
        }

        /// <summary>Returns the unread notification count for the bell badge.</summary>
        [HttpGet("GetUnreadCount")]
        public async Task<IActionResult> GetUnreadCount([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return BadRequest(new { message = "Username is required." });

            try
            {
                var count = await _notif.GetUnreadCount(username);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to get notification count.", detail = ex.Message });
            }
        }

        // ── Mark read ────────────────────────────────────────────────────────

        /// <summary>Marks a single notification as read.</summary>
        [HttpPost("MarkAsRead/{id}")]
        public async Task<IActionResult> MarkAsRead(int id, [FromBody] MarkReadDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.CurrentUser))
                return BadRequest(new { message = "Current user is required." });

            try
            {
                var (success, message) = await _notif.MarkAsRead(id, dto.CurrentUser);
                if (!success) return BadRequest(new { message });
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to mark notification as read.", detail = ex.Message });
            }
        }

        /// <summary>Marks all notifications for a user as read.</summary>
        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead([FromBody] MarkReadDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.CurrentUser))
                return BadRequest(new { message = "Current user is required." });

            try
            {
                var (success, message) = await _notif.MarkAllAsRead(dto.CurrentUser);
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to mark notifications as read.", detail = ex.Message });
            }
        }

        /// <summary>Soft-deletes (dismisses) a notification for a user.</summary>
        [HttpPost("DismissNotification/{id}")]
        public async Task<IActionResult> DismissNotification(int id, [FromBody] MarkReadDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.CurrentUser))
                return BadRequest(new { message = "Current user is required." });

            try
            {
                var (success, message) = await _notif.DismissNotification(id, dto.CurrentUser);
                if (!success) return BadRequest(new { message });
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to dismiss notification.", detail = ex.Message });
            }
        }

        // ── Low-stock alerts ─────────────────────────────────────────────────

        /// <summary>
        /// Returns all items currently below their reorder level across all warehouses.
        /// Uses StockLedger as the authoritative stock source — never ItemMaster.Quantity.
        /// </summary>
        [HttpGet("GetLowStockAlerts")]
        public async Task<IActionResult> GetLowStockAlerts()
        {
            try
            {
                var alerts = await _notif.GetCurrentLowStockAlerts();
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unable to load low stock alerts.", detail = ex.Message });
            }
        }
    }
}
