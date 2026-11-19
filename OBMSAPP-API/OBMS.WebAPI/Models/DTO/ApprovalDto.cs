namespace OBMS.WebAPI.Models.DTO
{
    // ── Shared action DTOs ──────────────────────────────────────────────────────

    /// <summary>Payload to submit a document for approval.</summary>
    public class SubmitDocumentDto
    {
        public string CurrentUser { get; set; } = string.Empty;
    }

    /// <summary>Payload to approve a document.</summary>
    public class ApproveDocumentDto
    {
        public string ApprovedBy { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }

    /// <summary>Payload to reject a document. RejectReason is mandatory.</summary>
    public class RejectDocumentDto
    {
        public string RejectedBy { get; set; } = string.Empty;
        public string RejectReason { get; set; } = string.Empty;
    }

    // ── Pending Approvals ───────────────────────────────────────────────────────

    /// <summary>Row displayed on the Pending Approvals screen.</summary>
    public class PendingApprovalDto
    {
        public string DocumentType { get; set; } = string.Empty;
        public string DocumentTypeDisplay { get; set; } = string.Empty;
        public int DocumentID { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime? SubmittedDate { get; set; }
        public string CurrentStatus { get; set; } = string.Empty;
        public string? WarehouseName { get; set; }
        public string? BranchName { get; set; }
        public decimal? TotalAmount { get; set; }
        /// <summary>Number of line items (used for PRs which have no monetary TotalAmount).</summary>
        public int ItemCount { get; set; }
        /// <summary>How long this document has been waiting (days).</summary>
        public int PendingSinceDays { get; set; }
    }

    // ── Approval History ────────────────────────────────────────────────────────

    /// <summary>Single history row for display.</summary>
    public class ApprovalHistoryDto
    {
        public int ID { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public int DocumentID { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ActionBy { get; set; } = string.Empty;
        public DateTime ActionDate { get; set; }
        public string? Comments { get; set; }
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
    }

    // ── Approval Configuration ──────────────────────────────────────────────────

    public class ApprovalConfigDto
    {
        public int ID { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool ApprovalRequired { get; set; }
        public string? ApproverRole { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateApprovalConfigDto
    {
        public bool ApprovalRequired { get; set; }
        public string? ApproverRole { get; set; }
        public string? Remarks { get; set; }
        public string LastUpdatedBy { get; set; } = string.Empty;
    }

    // ── PO extension DTOs (add Submitted state + RejectReason) ─────────────────

    /// <summary>Extends existing POApprovalDto — payload for reject PO.</summary>
    public class PORejectDto
    {
        public string RejectedBy { get; set; } = string.Empty;
        public string RejectReason { get; set; } = string.Empty;
    }

    // ── Approval Routing DTOs ───────────────────────────────────────────────────

    /// <summary>Row returned from ApprovalRouting table (list view).</summary>
    public class ApprovalRoutingDto
    {
        public int ID { get; set; }
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>User-friendly display name, e.g. "Purchase Requisition".</summary>
        public string DocumentTypeDisplay { get; set; } = string.Empty;

        public string RequesterUser { get; set; } = string.Empty;
        public string ApproverUser { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? LastUpdatedBy { get; set; }
        public DateTime LASTUPDATE { get; set; }
    }

    /// <summary>Payload for create / update of an ApprovalRouting row.</summary>
    public class SaveApprovalRoutingDto
    {
        /// <summary>0 for new; existing ID for update.</summary>
        public int ID { get; set; }

        /// <summary>PURCHASE_REQUISITION | PURCHASE_ORDER</summary>
        public string DocumentType { get; set; } = string.Empty;

        public string RequesterUser { get; set; } = string.Empty;
        public string ApproverUser { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string LastUpdatedBy { get; set; } = string.Empty;
    }
}
