namespace OBMS.WebAPI.Models.DTO
{
    // ════════════════════════════════════════════════════════════════════════════
    // WRITE DTOs  — Angular → API
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Payload sent from Angular when saving (create or update) a Stock Request.
    /// RequestedBy is stamped by the backend using the JWT identity;
    /// it is ignored if supplied by the client.
    /// </summary>
    public class StockRequestSaveDto
    {
        public int ID { get; set; }

        /// <summary>Auto-generated server-side; client may send empty string.</summary>
        public string? RequestNo { get; set; }

        public DateTime RequestDate { get; set; }

        public int RequestingBranchID { get; set; }

        public int SourceWarehouseID { get; set; }

        public string? Remarks { get; set; }

        /// <summary>Set by backend from JWT — not trusted from client.</summary>
        public string? LastUpdatedBy { get; set; }

        public List<StockRequestDetailSaveDto> Details { get; set; } = new();
    }

    public class StockRequestDetailSaveDto
    {
        public int ID { get; set; }
        public int ItemID { get; set; }
        public int? UOMID { get; set; }
        public decimal RequestedQty { get; set; }
        public decimal? BuyingPrice { get; set; }   // Auto-filled from ItemMaster; user-editable
        public int? ClientID { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Payload for Approve or Reject actions (mirrors PRApprovalDto pattern).
    /// ApprovedBy is stamped by the backend using the JWT identity.
    /// </summary>
    public class StockRequestApprovalDto
    {
        /// <summary>Filled by the backend from JWT — ignored if sent by client.</summary>
        public string ApprovedBy { get; set; } = string.Empty;

        /// <summary>Mandatory for Reject; optional for Approve.</summary>
        public string? ApprovalRemarks { get; set; }

        /// <summary>
        /// Set by the controller from the JWT "IsAdmin" claim (value "2" = superadmin).
        /// Never trusted from the client — always overridden server-side.
        /// BUG-05 FIX: replaces hard-coded "superadmin" username check in InventoryRepository.
        /// </summary>
        public bool IsSuperAdmin { get; set; } = false;
    }

    /// <summary>
    /// Payload sent when linking an approved Stock Request to a newly created
    /// Stock Transfer.
    /// </summary>
    public class StockRequestLinkTransferDto
    {
        public int LinkedTransferID { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // READ DTOs  — API → Angular
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Enriched row returned in the list view and the form edit/view mode.
    /// </summary>
    public class StockRequestListDto
    {
        public int ID { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }

        public int RequestingBranchID { get; set; }
        public string RequestingBranchName { get; set; } = string.Empty;

        public int SourceWarehouseID { get; set; }
        public string SourceWarehouseName { get; set; } = string.Empty;

        public string RequestedBy { get; set; } = string.Empty;

        /// <summary>Draft | Submitted | Approved | Rejected</summary>
        public string Status { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public string? SubmittedBy    { get; set; }
        public DateTime? SubmittedDate { get; set; }

        public string? ApprovedBy      { get; set; }
        public DateTime? ApprovedDate  { get; set; }
        public string? ApprovalRemarks { get; set; }

        public int? LinkedTransferID { get; set; }
        public string? LinkedTransferNo { get; set; }

        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        /// <summary>Enriched line items — populated by GetBSRByID.</summary>
        public List<StockRequestDetailListDto> Details { get; set; } = new();
    }

    /// <summary>
    /// Enriched line item returned by the read-side queries.
    /// Includes display names for Item and UOM so Angular does not need
    /// separate lookups.
    /// </summary>
    public class StockRequestDetailListDto
    {
        public int ID { get; set; }
        public int RequestID { get; set; }

        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;

        public int? UOMID { get; set; }
        public string UOMName { get; set; } = string.Empty;

        public decimal RequestedQty { get; set; }

        public decimal? BuyingPrice { get; set; }   // Cost price from ItemMaster; stored per line

        public int? ClientID { get; set; }
        public string ClientName { get; set; } = string.Empty;

        /// <summary>
        /// Available stock in the source warehouse at the time the form is loaded.
        /// Populated on demand via GetCurrentStock — not persisted.
        /// </summary>
        public decimal AvailableStock { get; set; }

        public string? Remarks { get; set; }
    }
}
