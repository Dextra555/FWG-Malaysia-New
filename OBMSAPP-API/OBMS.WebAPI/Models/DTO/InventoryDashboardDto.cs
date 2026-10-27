namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Root response for GET Inventory/GetInventoryDashboard.
    /// Populated from sp_InventoryDashboard (8 result sets).
    /// </summary>
    public class InventoryDashboardDto
    {
        // RS1: KPI Cards
        public int     TotalItems       { get; set; }
        public decimal TotalStockValue  { get; set; }
        public decimal TotalStockQty    { get; set; }
        public int     PendingTransfers { get; set; }
        public int     PendingIssues    { get; set; }
        public int     LowStockItems    { get; set; }

        // RS2: Warehouse donut chart
        public List<WarehouseStockValueDto> WarehouseStocks { get; set; } = new();

        // RS3: Monthly inward/outward bar chart
        public List<StockMovementDayDto> MonthlyMovements { get; set; } = new();

        // RS4: Global stock summary (single row)
        public WarehouseGlobalSummaryDto GlobalSummary { get; set; } = new();

        // RS5: Low stock alert items
        public List<LowStockAlertDto> LowStockAlerts { get; set; } = new();

        // RS6: Pending activities counts
        public PendingActivitiesDto PendingActivities { get; set; } = new();

        // RS7: Recent transactions
        public List<RecentTransactionDto> RecentTransactions { get; set; } = new();

        // RS8: Stock qty by warehouse – top 5 items (flat)
        public List<ItemWarehouseQtyDto> ItemWarehouseQty { get; set; } = new();
    }

    // ── RS2 ─────────────────────────────────────────────────────────────────
    public class WarehouseStockValueDto
    {
        public int     WarehouseID   { get; set; }
        public string  WarehouseName { get; set; } = string.Empty;
        public decimal StockValue    { get; set; }
        public decimal StockQty      { get; set; }
    }

    // ── RS3 ─────────────────────────────────────────────────────────────────
    public class StockMovementDayDto
    {
        public string  DateLabel  { get; set; } = string.Empty;
        public decimal InwardQty  { get; set; }
        public decimal OutwardQty { get; set; }
    }

    // ── RS4 ─────────────────────────────────────────────────────────────────
    public class WarehouseGlobalSummaryDto
    {
        public decimal OpeningQty  { get; set; }
        public decimal InwardQty   { get; set; }
        public decimal OutwardQty  { get; set; }
        public decimal ClosingQty  { get; set; }
        public decimal StockValue  { get; set; }
    }

    // ── RS5 ─────────────────────────────────────────────────────────────────
    // Reuses LowStockAlertDto defined in NotificationDto.cs

    // ── RS6 ─────────────────────────────────────────────────────────────────
    public class PendingActivitiesDto
    {
        public int POsPending          { get; set; }
        public int GRNsPending         { get; set; }
        public int TransfersPending    { get; set; }
        public int IssuesPending       { get; set; }
        public int AdjustmentsPending  { get; set; }
    }

    // ── RS7 ─────────────────────────────────────────────────────────────────
    public class RecentTransactionDto
    {
        public DateTime TxDate        { get; set; }
        public string   TxType        { get; set; } = string.Empty;
        public string   TxTypeCode    { get; set; } = string.Empty;
        public string?  RefNo         { get; set; }
        public string   ItemName      { get; set; } = string.Empty;
        public string   WarehouseName { get; set; } = string.Empty;
        public decimal  Qty           { get; set; }
    }

    // ── RS8 ─────────────────────────────────────────────────────────────────
    public class ItemWarehouseQtyDto
    {
        public string  ItemName      { get; set; } = string.Empty;
        public string  WarehouseName { get; set; } = string.Empty;
        public decimal Qty           { get; set; }
        public decimal TotalQty      { get; set; }
    }
}
