namespace OBMS.WebAPI.Models.DTO
{
    // ── Request DTOs ────────────────────────────────────────────────────────────

    /// <summary>Payload for Save/Update PR. ID=0 = new, ID>0 = update.</summary>
    public class PRRequestDto
    {
        public int ID { get; set; }
        public string? PRNumber { get; set; }          // generated server-side on new
        public DateTime PRDate { get; set; }
        public DateTime RequiredByDate { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<PRDetailRequestDto> Details { get; set; } = new();
    }

    public class PRDetailRequestDto
    {
        public int ID { get; set; }
        public int ItemID { get; set; }
        public decimal RequiredQty { get; set; }
        public decimal? BuyingPrice { get; set; }   // Auto-filled from ItemMaster; user-editable
        /// <summary>
        /// Estimated SST rate for this PR line (0, 6, 8, or any value).
        /// Carried to PurchaseOrderDetail.SSTPercent when a PO is raised from this PR.
        /// </summary>
        public decimal SSTPercent { get; set; } = 0;
        public int? UOMID { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    /// <summary>Payload for Approve or Reject action.</summary>
    public class PRApprovalDto
    {
        public string ApprovedBy { get; set; } = string.Empty;
        public string? ApprovalRemarks { get; set; }
        /// <summary>
        /// Set by the controller from the JWT "IsAdmin" claim (value "2" = superadmin).
        /// Never trusted from the client — always overridden server-side.
        /// BUG-05 FIX: replaces hard-coded "superadmin" username check in InventoryRepository.
        /// </summary>
        public bool IsSuperAdmin { get; set; } = false;
    }

    // ── List/Detail DTOs ────────────────────────────────────────────────────────

    /// <summary>Enriched PR row for list and detail screens.</summary>
    public class PRListDto
    {
        public int ID { get; set; }
        public string PRNumber { get; set; } = string.Empty;
        public DateTime PRDate { get; set; }
        public DateTime RequiredByDate { get; set; }
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Computed display status — extends Status with:
        ///   "PO Posted"  — Approved and a PO has been created
        ///   "GRN Posted" — Approved, PO created, and goods received
        /// Used by the PR list screen status badge.
        /// </summary>
        public string DisplayStatus { get; set; } = string.Empty;

        public string? Remarks { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? ApprovalRemarks { get; set; }
        /// <summary>
        /// The user who submitted this PR for approval (set by SubmitPR).
        /// Needed by the frontend to call IsCurrentUserApprover with the
        /// correct requesterUser so Approve / Reject buttons appear for the
        /// configured approver.
        /// </summary>
        public string? SubmittedBy { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<PRDetailListDto> Details { get; set; } = new();
    }

    public class PRDetailListDto
    {
        public int ID { get; set; }
        public int PRID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal RequiredQty { get; set; }
        public decimal? BuyingPrice { get; set; }   // Cost price from ItemMaster; stored per line
        /// <summary>Estimated SST rate carried from PR line.</summary>
        public decimal SSTPercent { get; set; }
        /// <summary>Estimated SST amount = RequiredQty × BuyingPrice × SSTPercent / 100.</summary>
        public decimal SSTAmount { get; set; }
        public int? UOMID { get; set; }
        public string UOMName { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }
}
