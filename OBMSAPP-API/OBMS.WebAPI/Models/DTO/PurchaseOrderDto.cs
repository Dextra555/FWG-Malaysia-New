namespace OBMS.WebAPI.Models.DTO
{
    // ── Request DTOs ────────────────────────────────────────────────────────────

    /// <summary>Payload for Save/Update PO. ID=0 = new, ID>0 = update.</summary>
    public class PORequestDto
    {
        public int ID { get; set; }
        public string? PONumber { get; set; }          // generated server-side on new
        public int? PRID { get; set; }                 // nullable – PO can be direct
        public int SupplierID { get; set; }
        public int WarehouseID { get; set; }
        public DateTime PODate { get; set; }
        public DateTime ExpectedDate { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<PODetailRequestDto> Details { get; set; } = new();
    }

    public class PODetailRequestDto
    {
        public int ID { get; set; }
        public int ItemID { get; set; }
        public int? UOMID { get; set; }
        public decimal OrderedQty { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    /// <summary>Payload for Approve PO action.</summary>
    public class POApprovalDto
    {
        public string ApprovedBy { get; set; } = string.Empty;
        /// <summary>
        /// Set by the controller from the JWT "IsAdmin" claim (value "2" = superadmin).
        /// Never trusted from the client — always overridden server-side.
        /// BUG-13 FIX: replaces hard-coded "superadmin" username check in legacy ApprovePO.
        /// </summary>
        public bool IsSuperAdmin { get; set; } = false;
    }

    // ── List/Detail DTOs ────────────────────────────────────────────────────────

    public class POListDto
    {
        public int ID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public int? PRID { get; set; }
        public string PRNumber { get; set; } = string.Empty;
        public int SupplierID { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime PODate { get; set; }
        public DateTime ExpectedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string? Remarks { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? SubmittedBy { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string? RejectReason { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<PODetailListDto> Details { get; set; } = new();
    }

    public class PODetailListDto
    {
        public int ID { get; set; }
        public int POID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int? UOMID { get; set; }
        public string UOMName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal PendingQty { get; set; }  // OrderedQty - ReceivedQty
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Remarks { get; set; }
    }

    /// <summary>
    /// Returned by GET /api/Inventory/GetPOEditability.
    /// Tells the frontend exactly what the user can and cannot change on a PO.
    /// </summary>
    public class POEditabilityDto
    {
        /// <summary>True when the PO form fields may be saved.</summary>
        public bool CanEdit { get; set; }

        /// <summary>
        /// True when this PO was created from a PR.
        /// When true, the item list is locked — only Qty and UnitPrice may change.
        /// </summary>
        public bool IsPRLinked { get; set; }

        /// <summary>PR number shown in the lock banner (only when IsPRLinked).</summary>
        public string? LinkedPRNumber { get; set; }

        /// <summary>Current PO status — used by the frontend to decide which buttons to show.</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>Why editing is blocked (only when CanEdit = false).</summary>
        public string? BlockReason { get; set; }
    }
}
