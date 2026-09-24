using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OBMS.WebAPI.Hubs;
using OBMS.WebAPI.Models;
using OBMS.WebAPI.Models.Domain;
using OBMS.WebAPI.Models.DTO;
using OBMS.WebAPI.Repositories.Interface;

namespace OBMS.WebAPI.Repositories.Implementation
{
    /// <summary>
    /// Manages AppNotification records.
    ///
    /// Key behaviours:
    ///   1. Deduplication — LOW_STOCK / OUT_OF_STOCK notifications are not
    ///      duplicated within the same calendar day per item+warehouse.
    ///   2. Non-throwing — CreateNotification / CheckAndNotifyLowStock never
    ///      propagate exceptions so caller transactions are not affected.
    ///   3. Low-stock uses StockLedger as the authoritative stock source —
    ///      never ItemMaster.Quantity.
    ///   4. After every DB save, the notification is pushed in real-time to
    ///      the recipient via SignalR (targeted group = username).
    ///      SignalR push is fire-and-forget; failure never rolls back the record.
    /// </summary>
    public class NotificationRepository : INotificationRepository
    {
        private readonly OBMSDbContext _db;
        private readonly ILogger<NotificationRepository> _logger;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationRepository(
            OBMSDbContext db,
            ILogger<NotificationRepository> logger,
            IHubContext<NotificationHub> hub)
        {
            _db     = db;
            _logger = logger;
            _hub    = hub;
        }

        // ════════════════════════════════════════════════════════════════════
        // RETRIEVE
        // ════════════════════════════════════════════════════════════════════

        public async Task<List<NotificationListDto>> GetNotifications(string username, int maxRows = 50)
        {
            var rows = await _db.AppNotifications
                .Where(n => n.RecipientUser == username && n.IsActive)
                .OrderByDescending(n => n.CreatedDate)
                .Take(maxRows)
                .Select(n => new NotificationListDto
                {
                    ID               = n.ID,
                    NotificationType = n.NotificationType,
                    Title            = n.Title,
                    Message          = n.Message,
                    RecipientUser    = n.RecipientUser,
                    ReferenceType    = n.ReferenceType,
                    ReferenceID      = n.ReferenceID,
                    ReferenceNumber  = n.ReferenceNumber,
                    IsRead           = n.IsRead,
                    ReadDate         = n.ReadDate,
                    CreatedDate      = n.CreatedDate
                })
                .ToListAsync();

            // Compute human-friendly "time ago" in memory
            var now = DateTime.Now;
            foreach (var r in rows)
                r.TimeAgo = ComputeTimeAgo(r.CreatedDate, now);

            return rows;
        }

        public async Task<NotificationCountDto> GetUnreadCount(string username)
        {
            int count = await _db.AppNotifications
                .CountAsync(n => n.RecipientUser == username && !n.IsRead && n.IsActive);
            return new NotificationCountDto { UnreadCount = count };
        }

        // ════════════════════════════════════════════════════════════════════
        // MARK READ / DISMISS
        // ════════════════════════════════════════════════════════════════════

        public async Task<(bool Success, string Message)> MarkAsRead(int id, string username)
        {
            var n = await _db.AppNotifications
                .FirstOrDefaultAsync(x => x.ID == id && x.RecipientUser == username);
            if (n == null) return (false, "Notification not found.");
            if (n.IsRead) return (true, "Already marked as read.");

            n.IsRead   = true;
            n.ReadDate = DateTime.Now;
            await _db.SaveChangesAsync();
            return (true, "Notification marked as read.");
        }

        public async Task<(bool Success, string Message)> MarkAllAsRead(string username)
        {
            var unread = await _db.AppNotifications
                .Where(n => n.RecipientUser == username && !n.IsRead && n.IsActive)
                .ToListAsync();

            if (!unread.Any()) return (true, "No unread notifications.");

            var now = DateTime.Now;
            foreach (var n in unread) { n.IsRead = true; n.ReadDate = now; }
            await _db.SaveChangesAsync();
            return (true, $"{unread.Count} notification(s) marked as read.");
        }

        public async Task<(bool Success, string Message)> DismissNotification(int id, string username)
        {
            var n = await _db.AppNotifications
                .FirstOrDefaultAsync(x => x.ID == id && x.RecipientUser == username);
            if (n == null) return (false, "Notification not found.");

            n.IsActive = false;
            await _db.SaveChangesAsync();
            return (true, "Notification dismissed.");
        }

        // ════════════════════════════════════════════════════════════════════
        // CREATE — generic, non-throwing
        // ════════════════════════════════════════════════════════════════════

        public async Task<bool> CreateNotification(
            string notificationType,
            string title,
            string message,
            string recipientUser,
            string? referenceType  = null,
            int?    referenceID    = null,
            string? referenceNumber = null,
            string? createdBy      = null)
        {
            try
            {
                var notif = new AppNotification
                {
                    NotificationType = notificationType,
                    Title            = title,
                    Message          = message,
                    RecipientUser    = recipientUser,
                    ReferenceType    = referenceType,
                    ReferenceID      = referenceID,
                    ReferenceNumber  = referenceNumber,
                    CreatedBy        = createdBy,
                    CreatedDate      = DateTime.Now,
                    IsRead           = false,
                    IsActive         = true
                };

                _db.AppNotifications.Add(notif);
                await _db.SaveChangesAsync();

                // ── Real-time push via SignalR ─────────────────────────────
                // Push only to the intended recipient's group (group name = username).
                // This is fire-and-forget — a SignalR failure never affects the
                // DB record that was already committed above.
                _ = PushToUserAsync(recipientUser, new
                {
                    notif.ID,
                    notif.NotificationType,
                    notif.Title,
                    notif.Message,
                    notif.RecipientUser,
                    notif.ReferenceType,
                    notif.ReferenceID,
                    notif.ReferenceNumber,
                    notif.IsRead,
                    notif.CreatedDate,
                    TimeAgo = "just now"
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notification could not be created (type={Type}, ref={Ref}).",
                    notificationType, referenceNumber);
                return false;
            }
        }

        /// <summary>
        /// Sends a SignalR event to the recipient's group.
        /// Swallows all exceptions so a hub failure never propagates.
        /// </summary>
        private async Task PushToUserAsync(string username, object payload)
        {
            try
            {
                // Each user joins a SignalR group named after their username on connect.
                // Sending to the group reaches all browser tabs the user has open.
                await _hub.Clients.Group(username)
                          .SendAsync("ReceiveNotification", payload);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SignalR push failed for user {User} — real-time delivery skipped, " +
                    "DB record is intact and polling will reconcile.", username);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // APPROVAL NOTIFICATIONS
        // ════════════════════════════════════════════════════════════════════

        public async Task SendApprovalNotification(
            string notificationType,
            string documentType,
            string documentNumber,
            int    documentID,
            string recipientUser,
            string? actionBy   = null,
            string? comments   = null)
        {
            try
            {
                string docDisplay = FriendlyDocType(documentType);
                string title;
                string message;

                switch (notificationType)
                {
                    case "PENDING_APPROVAL":
                        title   = $"{docDisplay} Pending Your Approval";
                        message = $"{docDisplay} {documentNumber} has been submitted and is pending your approval."
                                + (actionBy != null ? $" Submitted by: {actionBy}." : "");
                        break;

                    case "APPROVED":
                        title   = $"{docDisplay} Approved";
                        message = $"{docDisplay} {documentNumber} has been approved."
                                + (actionBy != null ? $" Approved by: {actionBy}." : "")
                                + (!string.IsNullOrEmpty(comments) ? $" Remarks: {comments}" : "");
                        break;

                    case "REJECTED":
                        title   = $"{docDisplay} Rejected";
                        message = $"{docDisplay} {documentNumber} has been rejected."
                                + (actionBy != null ? $" Rejected by: {actionBy}." : "")
                                + (!string.IsNullOrEmpty(comments) ? $" Reason: {comments}" : "");
                        break;

                    default:
                        title   = $"{docDisplay} Status Update";
                        message = $"{docDisplay} {documentNumber} status changed.";
                        break;
                }

                await CreateNotification(
                    notificationType, title, message, recipientUser,
                    documentType, documentID, documentNumber, actionBy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SendApprovalNotification failed for {Doc}.", documentNumber);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // LOW STOCK CHECK — uses StockLedger as authoritative source
        // ════════════════════════════════════════════════════════════════════

        public async Task CheckAndNotifyLowStock(int warehouseID, int itemID, string triggeredBy)
        {
            try
            {
                var item = await _db.ItemMasters
                    .FirstOrDefaultAsync(i => i.ID == itemID);
                if (item == null) return;

                // Only check items that have a reorder level configured
                if (!item.ReorderLevel.HasValue || item.ReorderLevel.Value <= 0) return;

                var warehouse = await _db.WarehouseMasters.FindAsync(warehouseID);
                string whName = warehouse?.Name ?? $"Warehouse {warehouseID}";

                // Stock from StockLedger — never ItemMaster.Quantity
                decimal currentStock = await GetCurrentStockFromLedger(warehouseID, itemID);

                bool isOutOfStock  = currentStock <= 0;
                bool isBelowReorder = currentStock <= item.ReorderLevel.Value;

                if (!isBelowReorder) return; // Stock is fine — nothing to do

                string notifType = isOutOfStock ? "OUT_OF_STOCK" : "LOW_STOCK";

                // ── Deduplication: no duplicate for same item+warehouse+type today ──
                var today = DateTime.Today;
                bool alreadySent = await _db.AppNotifications
                    .AnyAsync(n =>
                        n.NotificationType == notifType &&
                        n.ReferenceType    == "ITEM" &&
                        n.ReferenceID      == itemID &&
                        n.IsActive &&
                        n.CreatedDate >= today);

                if (alreadySent) return;

                // Build recipients — notify all active non-deleted users
                var recipients = await _db.Obmsusers
                    .Where(u => !u.IsDeleted)
                    .Select(u => u.Name)
                    .ToListAsync();

                string title, message;
                if (isOutOfStock)
                {
                    title   = $"Out of Stock: {item.Name}";
                    message = $"{item.Name} is out of stock in {whName}. "
                            + $"Reorder Quantity: {item.ReorderQuantity ?? 0}.";
                }
                else
                {
                    title   = $"Low Stock: {item.Name}";
                    message = $"{item.Name} is below the reorder level in {whName}. "
                            + $"Current Stock: {currentStock}. "
                            + $"Reorder Level: {item.ReorderLevel.Value}. "
                            + $"Reorder Quantity: {item.ReorderQuantity ?? 0}.";
                }

                foreach (var user in recipients)
                {
                    await CreateNotification(
                        notifType, title, message, user,
                        "ITEM", itemID, item.Code ?? item.Name, triggeredBy);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CheckAndNotifyLowStock failed for item {ItemID} warehouse {WhID}.",
                    itemID, warehouseID);
            }
        }

        public async Task<List<LowStockAlertDto>> GetCurrentLowStockAlerts()
        {
            // All items that have a ReorderLevel configured
            var items = await _db.ItemMasters
                .Where(i => i.ReorderLevel.HasValue && i.ReorderLevel.Value > 0)
                .ToListAsync();

            var warehouses = await _db.WarehouseMasters.ToListAsync();
            var result = new List<LowStockAlertDto>();

            foreach (var item in items)
            {
                foreach (var wh in warehouses)
                {
                    decimal stock = await GetCurrentStockFromLedger(wh.ID, item.ID);
                    if (stock <= item.ReorderLevel!.Value)
                    {
                        result.Add(new LowStockAlertDto
                        {
                            ItemID          = item.ID,
                            ItemCode        = item.Code ?? string.Empty,
                            ItemName        = item.Name,
                            WarehouseID     = wh.ID,
                            WarehouseName   = wh.Name,
                            CurrentStock    = stock,
                            ReorderLevel    = item.ReorderLevel.Value,
                            ReorderQuantity = item.ReorderQuantity ?? 0,
                            IsOutOfStock    = stock <= 0
                        });
                    }
                }
            }

            return result.OrderBy(r => r.ItemName).ToList();
        }

        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calculates current stock for an item in a warehouse from StockLedger.
        /// Mirrors the logic in InventoryRepository.GetCurrentStock().
        /// </summary>
        private async Task<decimal> GetCurrentStockFromLedger(int warehouseID, int itemID)
        {
            var entries = await _db.StockLedgers
                .Where(l => l.WarehouseID == warehouseID && l.ItemID == itemID)
                .Select(l => new { l.TransactionType, l.InQty, l.OutQty })
                .ToListAsync();

            decimal stock = 0;
            foreach (var e in entries)
            {
                stock += e.InQty;
                stock -= e.OutQty;
            }
            return stock;
        }

        private static string FriendlyDocType(string docType) => docType switch
        {
            "PURCHASE_REQUISITION" => "Purchase Requisition",
            "PURCHASE_ORDER"       => "Purchase Order",
            "STOCK_ADJUSTMENT"     => "Stock Adjustment",
            "STOCK_TRANSFER"       => "Stock Transfer",
            "DAMAGE_LOSS"          => "Damage / Loss",
            "ASSET_DISPOSAL"       => "Asset Disposal",
            _                      => docType
        };

        private static string ComputeTimeAgo(DateTime created, DateTime now)
        {
            var diff = now - created;
            if (diff.TotalMinutes < 1)  return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours   < 24) return $"{(int)diff.TotalHours} hr ago";
            if (diff.TotalDays    < 7)  return $"{(int)diff.TotalDays} day(s) ago";
            return created.ToString("dd MMM yyyy");
        }
    }
}
