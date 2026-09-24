using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("PurchaseOrder")]
    public class PurchaseOrder
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated: PO0001, PO0002 …</summary>
        public string PONumber { get; set; } = string.Empty;

        /// <summary>Nullable — PO can be created without a PR.</summary>
        public int? PRID { get; set; }

        public int SupplierID { get; set; }

        public int WarehouseID { get; set; }

        public DateTime PODate { get; set; }

        public DateTime ExpectedDate { get; set; }

        /// <summary>
        /// Draft → Submitted → Approved → Partially Received → Fully Received
        ///                   → Rejected
        ///                   (or Cancelled from Draft/Submitted/Approved)
        /// When no ApprovalRouting is configured for the submitter, Submit goes
        /// directly to Approved (no Submitted state).
        /// </summary>
        public string Status { get; set; } = "Draft";

        public decimal TotalAmount { get; set; }

        public string? Remarks { get; set; }

        public string? SubmittedBy { get; set; }

        public DateTime? SubmittedDate { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        /// <summary>Rejection reason filled by approver.</summary>
        public string? RejectReason { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation
        [JsonIgnore]
        [ForeignKey("PRID")]
        public virtual PurchaseRequisition? PR { get; set; }

        [JsonIgnore]
        [ForeignKey("SupplierID")]
        public virtual Supplier? Supplier { get; set; }

        [JsonIgnore]
        [ForeignKey("WarehouseID")]
        public virtual WarehouseMaster? Warehouse { get; set; }

        [JsonIgnore]
        public virtual ICollection<PurchaseOrderDetail> Details { get; set; }
            = new List<PurchaseOrderDetail>();
    }
}
