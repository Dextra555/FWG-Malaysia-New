namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Enriched ledger row returned for the Stock Ledger screen.
    /// RunningBalance is calculated as a cumulative sum by the API query.
    /// </summary>
    public class StockLedgerRowDto
    {
        public int ID { get; set; }
        public string TransactionNo { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;

        /// <summary>User-friendly label for the transaction type, e.g. "Opening Stock"</summary>
        public string TransactionTypeLabel { get; set; } = string.Empty;

        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string UOMName { get; set; } = string.Empty;
        public decimal InQty { get; set; }
        public decimal OutQty { get; set; }

        /// <summary>Cumulative running balance up to and including this row</summary>
        public decimal RunningBalance { get; set; }

        public decimal? UnitCost { get; set; }
        public string? ReferenceNo { get; set; }
        public string? ReferenceType { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
