namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Enriched row returned in the Stock Transfer list and detail views.
    /// </summary>
    public class StockTransferListDto
    {
        public int ID { get; set; }
        public string TransferNo { get; set; } = string.Empty;
        public DateTime TransferDate { get; set; }
        public int SourceWarehouseID { get; set; }
        public string SourceWarehouseName { get; set; } = string.Empty;
        public int DestWarehouseID { get; set; }
        public string DestWarehouseName { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<StockTransferDetailListDto> Details { get; set; } = new();
    }

    public class StockTransferDetailListDto
    {
        public int ID { get; set; }
        public int TransferID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string UOMName { get; set; } = string.Empty;
        public decimal TransferQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string? Remarks { get; set; }
    }
}
