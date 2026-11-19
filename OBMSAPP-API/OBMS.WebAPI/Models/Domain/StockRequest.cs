using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Header record for a Branch Stock Request.
    /// A branch raises this document to request items from a source warehouse.
    /// Flow: Draft → Submitted → Approved | Rejected.
    /// Once Approved, a normal StockTransfer is created and linked here via
    /// LinkedTransferID.
    /// </summary>
    [Table("StockRequest")]
    public class StockRequest
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated reference number, e.g. SR0001</summary>
        public string RequestNo { get; set; } = string.Empty;

        public DateTime RequestDate { get; set; }

        /// <summary>FK → BranchMaster. The branch asking for stock.</summary>
        public int RequestingBranchID { get; set; }

        /// <summary>FK → WarehouseMaster. The warehouse that will fulfil the request.</summary>
        public int SourceWarehouseID { get; set; }

        /// <summary>Username of the person who created the request (set by backend from JWT).</summary>
        public string RequestedBy { get; set; } = string.Empty;

        /// <summary>Draft | Submitted | Approved | Rejected</summary>
        public string Status { get; set; } = "Draft";

        public string? Remarks { get; set; }

        // ── Submission tracking ──────────────────────────────────────────────
        public string? SubmittedBy   { get; set; }
        public DateTime? SubmittedDate { get; set; }

        // ── Approval tracking ────────────────────────────────────────────────
        public string? ApprovedBy      { get; set; }
        public DateTime? ApprovedDate  { get; set; }
        public string? ApprovalRemarks { get; set; }

        // ── Transfer link ────────────────────────────────────────────────────
        /// <summary>
        /// Set once the approved request is fulfilled via an existing StockTransfer.
        /// Null until transfer is created.
        /// </summary>
        public int? LinkedTransferID { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        // ── Navigation (ignored in serialisation to prevent cycles) ──────────
        [JsonIgnore]
        public virtual BranchMaster?  RequestingBranch  { get; set; }

        [JsonIgnore]
        public virtual WarehouseMaster? SourceWarehouse { get; set; }

        [JsonIgnore]
        public virtual StockTransfer? LinkedTransfer { get; set; }

        [JsonIgnore]
        public virtual ICollection<StockRequestDetail> Details { get; set; }
            = new List<StockRequestDetail>();
    }
}
