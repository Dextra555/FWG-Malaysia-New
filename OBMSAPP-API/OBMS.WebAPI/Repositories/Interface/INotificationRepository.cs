using OBMS.WebAPI.Models.DTO;

namespace OBMS.WebAPI.Repositories.Interface
{
    public interface INotificationRepository
    {
        // ── Retrieve ─────────────────────────────────────────────────────────
        Task<List<NotificationListDto>> GetNotifications(string username, int maxRows = 50);
        Task<NotificationCountDto> GetUnreadCount(string username);

        // ── Mark read ────────────────────────────────────────────────────────
        Task<(bool Success, string Message)> MarkAsRead(int id, string username);
        Task<(bool Success, string Message)> MarkAllAsRead(string username);
        Task<(bool Success, string Message)> DismissNotification(int id, string username);

        // ── Create ───────────────────────────────────────────────────────────

        /// <summary>
        /// Generic notification creator — called from approval/stock operations.
        /// Returns false (and does NOT throw) if the notification cannot be created,
        /// so the caller's transaction is never rolled back by a notification failure.
        /// </summary>
        Task<bool> CreateNotification(
            string notificationType,
            string title,
            string message,
            string recipientUser,
            string? referenceType = null,
            int? referenceID = null,
            string? referenceNumber = null,
            string? createdBy = null);

        /// <summary>
        /// Send an approval-event notification to a user.
        /// Used by InventoryRepository after approve / reject / submit actions.
        /// </summary>
        Task SendApprovalNotification(
            string notificationType,   // PENDING_APPROVAL | APPROVED | REJECTED
            string documentType,
            string documentNumber,
            int documentID,
            string recipientUser,
            string? actionBy = null,
            string? comments = null);

        // ── Low-stock check ──────────────────────────────────────────────────

        /// <summary>
        /// Check a single item's stock against its ReorderLevel.
        /// If below reorder level, create a LOW_STOCK or OUT_OF_STOCK notification
        /// (deduplicated — no duplicate created within the same day).
        /// Recipient is 'all' (broadcast to all active users) or a specific username.
        /// </summary>
        Task CheckAndNotifyLowStock(int warehouseID, int itemID, string triggeredBy);

        /// <summary>Run a full low-stock sweep across all warehouses and items with ReorderLevel set.</summary>
        Task<List<LowStockAlertDto>> GetCurrentLowStockAlerts();
    }
}
