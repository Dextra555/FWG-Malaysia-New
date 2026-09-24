namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Enriched row returned in the Stock Issue list and detail views.
    /// </summary>
    public class StockIssueDocListDto
    {
        public int ID { get; set; }
        public string IssueNo { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string IssueToType { get; set; } = string.Empty;
        public int? IssueToID { get; set; }
        public string? IssueToName { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public List<StockIssueDocDetailListDto> Details { get; set; } = new();
    }

    public class StockIssueDocDetailListDto
    {
        public int ID { get; set; }
        public int IssueDocID { get; set; }
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int? UOMID { get; set; }
        public string UOMName { get; set; } = string.Empty;
        public decimal IssueQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }
        public string? Remarks { get; set; }
        /// <summary>Pre-computed: how much of this line has already been returned.</summary>
        public decimal AlreadyReturnedQty { get; set; }
        /// <summary>IssueQty - AlreadyReturnedQty</summary>
        public decimal ReturnableQty { get; set; }
    }
}
