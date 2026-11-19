using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Application notification directed at a specific user.
    /// Supports read/unread state, soft-delete, and deduplication via ReferenceType+ReferenceID.
    /// </summary>
    [Table("AppNotification")]
    public class AppNotification
    {
        [Key]
        public int ID { get; set; }

        /// <summary>PENDING_APPROVAL | APPROVED | REJECTED | LOW_STOCK | OUT_OF_STOCK</summary>
        [Required, MaxLength(50)]
        public string NotificationType { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>Username of the intended recipient.</summary>
        [Required, MaxLength(100)]
        public string RecipientUser { get; set; } = string.Empty;

        /// <summary>PURCHASE_REQUISITION | PURCHASE_ORDER | ITEM | …</summary>
        [MaxLength(50)]
        public string? ReferenceType { get; set; }

        /// <summary>Primary key of the referenced object (DocumentID or ItemID).</summary>
        public int? ReferenceID { get; set; }

        /// <summary>Human-readable: PR0001, PO0001, item code, …</summary>
        [MaxLength(50)]
        public string? ReferenceNumber { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadDate { get; set; }

        /// <summary>Soft-delete flag — false = dismissed by user.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }
    }
}
