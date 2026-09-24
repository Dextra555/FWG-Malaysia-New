namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload sent from Angular when saving a new Stock Issue document.
    /// ID = 0 means new record.
    /// Each detail line reduces stock for the selected item in the selected warehouse.
    /// </summary>
    public class StockIssueDocRequestDto
    {
        public int ID { get; set; }

        /// <summary>IssueNo is generated server-side on new; preserved on view/cancel.</summary>
        public string? IssueNo { get; set; }

        public DateTime IssueDate { get; set; }

        public int WarehouseID { get; set; }

        /// <summary>Employee | Guard | Branch | Other</summary>
        public string IssueToType { get; set; } = string.Empty;

        /// <summary>FK ID for Employee/Branch (null for Other)</summary>
        public int? IssueToID { get; set; }

        /// <summary>Free-text or resolved name for the recipient</summary>
        public string? IssueToName { get; set; }

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }

        public List<StockIssueDocDetailRequestDto> Details { get; set; } = new();
    }

    public class StockIssueDocDetailRequestDto
    {
        public int ID { get; set; }

        public int ItemID { get; set; }

        public int? UOMID { get; set; }

        public decimal IssueQty { get; set; }

        public decimal UnitCost { get; set; }

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }
    }
}
