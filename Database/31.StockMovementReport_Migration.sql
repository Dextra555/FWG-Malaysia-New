-- ============================================================
-- Migration 31: Stock Movement Report
-- Creates sp_StockMovementReport stored procedure
-- and registers the screen in ScreenList for access control.
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_StockMovementReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_StockMovementReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
--
-- Stock Movement Report: summarises total IN / OUT movements
-- per warehouse + item within a date range.
-- Unlike the Stock Ledger (row-per-transaction), this SP groups
-- by warehouse + item and shows aggregate movement totals,
-- opening balance, and closing balance for the period.
-- ─────────────────────────────────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_StockMovementReport
    @WarehouseID INT          = 0,
    @ItemID      INT          = 0,
    @FromDate    DATETIME     = NULL,
    @ToDate      DATETIME     = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Extend ToDate to end of day (23:59:59) — same pattern as sp_InventoryStockLedger
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    SELECT
        wh.ID                                               AS WarehouseID,
        wh.Name                                             AS WarehouseName,
        im.ID                                               AS ItemID,
        im.Code                                             AS ItemCode,
        im.Name                                             AS ItemName,
        ISNULL(ic.Name, '')                                 AS CategoryName,
        ISNULL(bm.Name, '')                                 AS BrandName,
        ISNULL(um.Name, '')                                 AS UOMName,

        -- Opening Balance: sum of ALL movements BEFORE @FromDate
        ISNULL((
            SELECT SUM(sl_open.InQty - sl_open.OutQty)
            FROM   StockLedger sl_open
            WHERE  sl_open.WarehouseID = sl.WarehouseID
              AND  sl_open.ItemID      = sl.ItemID
              AND  sl_open.TransactionDate < @FromDate
        ), 0)                                               AS OpeningBalance,

        -- Total IN within period
        SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                 THEN sl.InQty ELSE 0 END)                 AS TotalIn,

        -- Total OUT within period
        SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                 THEN sl.OutQty ELSE 0 END)                AS TotalOut,

        -- Closing Balance: OpeningBalance + TotalIn - TotalOut
        ISNULL((
            SELECT SUM(sl_open.InQty - sl_open.OutQty)
            FROM   StockLedger sl_open
            WHERE  sl_open.WarehouseID = sl.WarehouseID
              AND  sl_open.ItemID      = sl.ItemID
              AND  sl_open.TransactionDate < @FromDate
        ), 0)
        + SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                   THEN sl.InQty ELSE 0 END)
        - SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                   THEN sl.OutQty ELSE 0 END)              AS ClosingBalance,

        -- Last Unit Cost
        (
            SELECT TOP 1 sl2.UnitCost
            FROM   StockLedger sl2
            WHERE  sl2.WarehouseID = sl.WarehouseID
              AND  sl2.ItemID      = sl.ItemID
              AND  sl2.UnitCost IS NOT NULL
              AND  sl2.UnitCost > 0
              AND  sl2.TransactionDate <= @ToDate
            ORDER BY sl2.TransactionDate DESC, sl2.ID DESC
        )                                                   AS LastUnitCost,

        -- Transaction count within period
        COUNT(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                   THEN 1 END)                             AS TxnCount

    FROM StockLedger sl
        INNER JOIN WarehouseMaster  wh ON sl.WarehouseID = wh.ID
        INNER JOIN ItemMaster       im ON sl.ItemID      = im.ID
        LEFT  JOIN InventoryCategory ic ON im.CategoryID = ic.ID
        LEFT  JOIN BrandMaster      bm ON im.BrandID     = bm.ID
        LEFT  JOIN UOMMaster        um ON im.UOMID       = um.ID
    WHERE wh.IsActive = 1
      AND (@WarehouseID = 0 OR sl.WarehouseID = @WarehouseID)
      AND (@ItemID = 0 OR sl.ItemID = @ItemID)
    GROUP BY
        sl.WarehouseID, sl.ItemID,
        wh.ID, wh.Name,
        im.ID, im.Code, im.Name, im.ReorderLevel,
        ic.Name, bm.Name, um.Name
    HAVING
        -- Only include rows that had activity OR had a non-zero balance during period
        SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                 THEN sl.InQty + sl.OutQty ELSE 0 END) > 0
        OR
        ISNULL((
            SELECT SUM(sl_open.InQty - sl_open.OutQty)
            FROM   StockLedger sl_open
            WHERE  sl_open.WarehouseID = sl.WarehouseID
              AND  sl_open.ItemID      = sl.ItemID
              AND  sl_open.TransactionDate < @FromDate
        ), 0) <> 0
    ORDER BY
        wh.Name ASC,
        im.Name ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Movement Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Movement Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 31: sp_StockMovementReport and ScreenList entry created successfully.';
GO
