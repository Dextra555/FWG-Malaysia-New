using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Tracks the Purchase department's work queue for approved Purchase Requisitions.
    ///
    /// Lifecycle:
    ///   Pending   → created when a PR is approved (one row per PR, UNIQUE on PRID)
    ///   Claimed   → a Purchase user takes ownership via the atomic ClaimPR endpoint
    ///   Completed → the linked PO is created; stamped with PO number and actor
    ///
    /// The UNIQUE constraint on PRID is enforced at the DB level.
    /// The Claim operation uses a conditional UPDATE (ExecuteUpdateAsync WHERE Status='Pending')
    /// so only one concurrent caller wins — no explicit locking required.
    /// </summary>
    [Table("PRProcessingTask")]
    public class PRProcessingTask
    {
        [Key]
        public int ID { get; set; }

        /// <summary>FK to PurchaseRequisition.ID. Unique — one task per PR.</summary>
        public int PRID { get; set; }

        /// <summary>Denormalised PR number (e.g. PR0001) — avoids a join on every inbox query.</summary>
        [Required, MaxLength(50)]
        public string PRNumber { get; set; } = string.Empty;

        /// <summary>Username of the Purchase user who claimed this task. Null = unclaimed.</summary>
        [MaxLength(100)]
        public string? ClaimedBy { get; set; }

        public DateTime? ClaimedDate { get; set; }

        /// <summary>Pending | Claimed | Completed</summary>
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";

        /// <summary>Username of the user who created the PO (task completion actor).</summary>
        [MaxLength(100)]
        public string? CompletedBy { get; set; }

        public DateTime? CompletedDate { get; set; }

        /// <summary>PO number that fulfilled this requisition task (e.g. PO0001).</summary>
        [MaxLength(50)]
        public string? PONumber { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        public DateTime LASTUPDATE { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? LastUpdatedBy { get; set; }
    }
}
