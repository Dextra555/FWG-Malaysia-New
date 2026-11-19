namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Enriched row returned in the Stock Issue Return list and detail views.
    /// </summary>
    public class StockIssueReturnListDto
    {
        public int ID { get; set; }
        public string ReturnNo { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public int OriginalIssueDocID { get; set; }
        public string OriginalIssueNo { get; set; } = string.Empty;
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? IssueToName { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<StockIssueReturnDetailListDto> Details { get; set; } = new();
    }

    public class StockIssueReturnDetailListDto
    {
        public int ID { get; set; }
        public int ReturnID { get; set; }
        public int OriginalIssueDetailID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string UOMName { get; set; } = string.Empty;
        public decimal ReturnQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string? Remarks { get; set; }
    }
}
