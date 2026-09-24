namespace OBMS.WebAPI.Models.DTO
{
    // ── Request DTOs ────────────────────────────────────────────────────────────

    /// <summary>Payload for Save Purchase Return.</summary>
    public class PurchaseReturnRequestDto
    {
        public int ID { get; set; }
        public string? ReturnNumber { get; set; }      // generated server-side on new
        public int GRNID { get; set; }
        public int WarehouseID { get; set; }
        public DateTime ReturnDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? LastUpdatedBy { get; set; }
        public List<PurchaseReturnDetailRequestDto> Details { get; set; } = new();
    }

    public class PurchaseReturnDetailRequestDto
    {
        public int ID { get; set; }
        public int GRNDetailID { get; set; }
        public int ItemID { get; set; }
        public int? UOMID { get; set; }
        public decimal ReturnQty { get; set; }
        public decimal UnitCost { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    // ── List/Detail DTOs ────────────────────────────────────────────────────────

    public class PurchaseReturnListDto
    {
        public int ID { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int GRNID { get; set; }
        public string GRNNumber { get; set; } = string.Empty;
        public string PONumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<PurchaseReturnDetailListDto> Details { get; set; } = new();
    }

    public class PurchaseReturnDetailListDto
    {
        public int ID { get; set; }
        public int ReturnID { get; set; }
        public int GRNDetailID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int? UOMID { get; set; }
        public string UOMName { get; set; } = string.Empty;
        public decimal ReturnQty { get; set; }
        public decimal UnitCost { get; set; }
    }

    /// <summary>
    /// Used to populate the Return form from a GRN.
    /// Shows each GRN line with returnable quantity.
    /// </summary>
    public class GRNLineForReturnDto
    {
        public int GRNDetailID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int? UOMID { get; set; }
        public string UOMName { get; set; } = string.Empty;
        public decimal ReceivedQty { get; set; }
        public decimal AlreadyReturnedQty { get; set; }
        public decimal ReturnableQty { get; set; }    // ReceivedQty - AlreadyReturnedQty
        public decimal UnitCost { get; set; }
    }
}
