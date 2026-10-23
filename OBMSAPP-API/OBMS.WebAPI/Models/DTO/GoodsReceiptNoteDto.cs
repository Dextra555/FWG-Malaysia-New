namespace OBMS.WebAPI.Models.DTO
{
    // ── Request DTOs ────────────────────────────────────────────────────────────

    /// <summary>Payload for Save GRN. Each detail line represents received qty for one PO line.</summary>
    public class GRNRequestDto
    {
        public int ID { get; set; }
        public string? GRNNumber { get; set; }         // generated server-side on new
        public int POID { get; set; }
        public int WarehouseID { get; set; }
        public DateTime GRNDate { get; set; }
        public string? DeliveryNoteRef { get; set; }
        public string? SupplierInvoiceNo { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<GRNDetailRequestDto> Details { get; set; } = new();
    }

    public class GRNDetailRequestDto
    {
        public int ID { get; set; }
        public int PODetailID { get; set; }
        public int ItemID { get; set; }
        public int? UOMID { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal UnitCost { get; set; }
        /// <summary>
        /// Carried automatically from PurchaseOrderDetail.SSTPercent.
        /// Not entered by the user on the GRN form.
        /// </summary>
        public decimal SSTPercent { get; set; } = 0;
        public string? LastUpdatedBy { get; set; }
    }

    // ── List/Detail DTOs ────────────────────────────────────────────────────────

    public class GRNListDto
    {
        public int ID { get; set; }
        public string GRNNumber { get; set; } = string.Empty;
        public int POID { get; set; }
        public string PONumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime GRNDate { get; set; }
        public string? DeliveryNoteRef { get; set; }
        public string? SupplierInvoiceNo { get; set; }
        public string? Remarks { get; set; }
        /// <summary>Sum of all GRN line SSTAmount values.</summary>
        public decimal SSTTotal { get; set; }
        /// <summary>Σ(ReceivedQty × UnitCost) + SSTTotal. Written to CreditorInvoice.Total.</summary>
        public decimal GrandTotal { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<GRNDetailListDto> Details { get; set; } = new();
    }

    public class GRNDetailListDto
    {
        public int ID { get; set; }
        public int GRNID { get; set; }
        public int PODetailID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int? UOMID { get; set; }
        public string UOMName { get; set; } = string.Empty;
        public decimal ReceivedQty { get; set; }
        public decimal UnitCost { get; set; }
        /// <summary>SST rate carried from the PO line.</summary>
        public decimal SSTPercent { get; set; }
        /// <summary>Stored SST amount = ReceivedQty × UnitCost × SSTPercent / 100.</summary>
        public decimal SSTAmount { get; set; }
    }

    /// <summary>
    /// Used to populate the GRN form from an approved PO.
    /// Shows each PO line with pending quantity available for receiving.
    /// </summary>
    public class POLineForGRNDto
    {
        public int PODetailID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int? UOMID { get; set; }
        public string UOMName { get; set; } = string.Empty;
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal PendingQty { get; set; }   // OrderedQty - ReceivedQty
        public decimal UnitCost { get; set; }      // from PO UnitPrice
        /// <summary>SST rate from the PO line — displayed read-only on GRN form.</summary>
        public decimal SSTPercent { get; set; }
    }
}
