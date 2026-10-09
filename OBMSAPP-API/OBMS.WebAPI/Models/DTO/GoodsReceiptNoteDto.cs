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
    }
}
