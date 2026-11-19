using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Immutable audit trail — one row per approve / reject / submit / cancel action.
    /// Records are never updated or deleted.
    /// </summary>
    [Table("ApprovalHistory")]
    public class ApprovalHistory
    {
        [Key]
        public int ID { get; set; }

        /// <summary>PURCHASE_REQUISITION | PURCHASE_ORDER | STOCK_ADJUSTMENT | …</summary>
        [Required, MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>Primary key of the source document.</summary>
        public int DocumentID { get; set; }

        /// <summary>Human-readable: PR0001, PO0001, …</summary>
        [MaxLength(50)]
        public string DocumentNumber { get; set; } = string.Empty;

        /// <summary>Submitted | Approved | Rejected | Cancelled</summary>
        [Required, MaxLength(30)]
        public string Action { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string ActionBy { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; } = DateTime.Now;

        /// <summary>Approval remarks or rejection reason.</summary>
        [MaxLength(1000)]
        public string? Comments { get; set; }

        [MaxLength(50)]
        public string? PreviousStatus { get; set; }

        [MaxLength(50)]
        public string? NewStatus { get; set; }
    }
}
