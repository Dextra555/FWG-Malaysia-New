namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload sent from Angular when saving a Stock Issue Return.
    /// Each detail line references a specific issue detail line.
    /// ReturnQty per line must not exceed (IssueQty - already returned qty).
    /// </summary>
    public class StockIssueReturnRequestDto
    {
        public int ID { get; set; }

        /// <summary>ReturnNo is generated server-side.</summary>
        public string? ReturnNo { get; set; }

        public DateTime ReturnDate { get; set; }

        /// <summary>FK to the original StockIssueDoc</summary>
        public int OriginalIssueDocID { get; set; }

        /// <summary>Warehouse where stock is returned to</summary>
        public int WarehouseID { get; set; }

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }

        public List<StockIssueReturnDetailRequestDto> Details { get; set; } = new();
    }

    public class StockIssueReturnDetailRequestDto
    {
        public int ID { get; set; }

        /// <summary>FK to the original StockIssueDocDetail line being returned</summary>
        public int OriginalIssueDetailID { get; set; }

        public int ItemID { get; set; }

        public int? UOMID { get; set; }

        public decimal ReturnQty { get; set; }

        public decimal UnitCost { get; set; }

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }
    }
}
