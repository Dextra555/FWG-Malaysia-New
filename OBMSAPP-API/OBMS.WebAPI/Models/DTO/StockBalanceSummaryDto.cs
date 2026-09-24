namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Current stock balance for one Item in one Warehouse.
    /// Computed as SUM(InQty) - SUM(OutQty) from StockLedger.
    /// Never reads ItemMaster.Quantity — ledger is the single source of truth.
    /// </summary>
    public class StockBalanceSummaryDto
    {
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string UOMName { get; set; } = string.Empty;
        public string UOMShort { get; set; } = string.Empty;

        public decimal CurrentStock { get; set; }

        /// <summary>From ItemMaster — used only for display/comparison, not stock calculation</summary>
        public decimal? ReorderLevel { get; set; }

        /// <summary>True when CurrentStock <= ReorderLevel</summary>
        public bool BelowReorderLevel { get; set; }

        /// <summary>Last unit cost recorded in the ledger for this item/warehouse</summary>
        public decimal? LastUnitCost { get; set; }

        /// <summary>CurrentStock × LastUnitCost</summary>
        public decimal StockValue { get; set; }

        public DateTime? LastMovementDate { get; set; }
        public string? LastTransactionType { get; set; }
    }
}
