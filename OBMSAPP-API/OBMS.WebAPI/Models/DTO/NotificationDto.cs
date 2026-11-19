namespace OBMS.WebAPI.Models.DTO
{
    // ── Notification list / detail ──────────────────────────────────────────────

    public class NotificationListDto
    {
        public int ID { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RecipientUser { get; set; } = string.Empty;
        public string? ReferenceType { get; set; }
        public int? ReferenceID { get; set; }
        public string? ReferenceNumber { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }
        public DateTime CreatedDate { get; set; }
        /// <summary>Human-friendly time-ago, e.g. "2 hours ago".</summary>
        public string TimeAgo { get; set; } = string.Empty;
    }

    // ── Unread count ────────────────────────────────────────────────────────────

    public class NotificationCountDto
    {
        public int UnreadCount { get; set; }
    }

    // ── Mark read ───────────────────────────────────────────────────────────────

    public class MarkReadDto
    {
        public string CurrentUser { get; set; } = string.Empty;
    }

    // ── Low-stock alert summary (used internally + by API) ──────────────────────

    public class LowStockAlertDto
    {
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal ReorderQuantity { get; set; }
        public bool IsOutOfStock { get; set; }
    }
}
