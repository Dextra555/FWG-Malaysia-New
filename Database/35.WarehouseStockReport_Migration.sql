-- ============================================================
-- Migration 35: Warehouse-wise Stock Report
-- Creates sp_WarehouseStockReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report shows current stock balance grouped by
-- Warehouse → Item, showing TotalIn, TotalOut, ClosingBalance,
-- LastUnitCost, and StockValue.
-- Filtered by Warehouse, Category, and/or Item (all optional).
-- As-of-date filter to see stock at any point in time.
--
-- Table reference:
--   StockLedger     : WarehouseID, ItemID, InQty, OutQty,
--                     UnitCost, TransactionDate
--   WarehouseMaster : ID, Name, IsActive
--   ItemMaster      : ID, Code, Name, CategoryID, BrandID, UOMID
-- ============================================================

USE [OBMSUATNEW]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_WarehouseStockReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_WarehouseStockReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_WarehouseStockReport
    @WarehouseID INT      = 0,       -- 0 = All Warehouses
    @CategoryID  INT      = 0,       -- 0 = All Categories
    @ItemID      INT      = 0,       -- 0 = All Items
    @AsOfDate    DATETIME = NULL     -- NULL = today
AS
BEGIN
    SET NOCOUNT ON;

    -- Default AsOfDate to today if not provided
    IF @AsOfDate IS NULL
        SET @AsOfDate = CAST(GETDATE() AS DATE);

    -- Extend to end of day (23:59:59) so full day is included
    SET @AsOfDate = DATEADD(SECOND, 86399,
                        CAST(CAST(@AsOfDate AS DATE) AS DATETIME));

    SELECT
        wh.ID                                               AS WarehouseID,
        wh.Name                                             AS WarehouseName,
        ISNULL(ic.Name, 'Uncategorised')                    AS CategoryName,
        im.ID                                               AS ItemID,
        im.Code                                             AS ItemCode,
        im.Name                                             AS ItemName,
        ISNULL(bm.Name, '')                                 AS BrandName,
        ISNULL(um.Name, '')                                 AS UOMName,

        -- Aggregate movements up to AsOfDate
        ISNULL(SUM(sl.InQty),  0)                           AS TotalIn,
        ISNULL(SUM(sl.OutQty), 0)                           AS TotalOut,

        -- Closing balance = TotalIn − TotalOut
        ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0)
                                                            AS ClosingBalance,

        ISNULL(im.ReorderLevel, 0)                          AS ReorderLevel,

        -- Last unit cost (most recent non-zero cost up to AsOfDate)
        (
            SELECT TOP 1 sl2.UnitCost
            FROM   StockLedger sl2
            WHERE  sl2.WarehouseID     = sl.WarehouseID
              AND  sl2.ItemID          = sl.ItemID
              AND  sl2.UnitCost IS NOT NULL
              AND  sl2.UnitCost        > 0
              AND  sl2.TransactionDate <= @AsOfDate
            ORDER BY sl2.TransactionDate DESC, sl2.ID DESC
        )                                                   AS LastUnitCost,

        -- Stock value = ClosingBalance × LastUnitCost
        (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0))
        * ISNULL((
            SELECT TOP 1 sl3.UnitCost
            FROM   StockLedger sl3
            WHERE  sl3.WarehouseID     = sl.WarehouseID
              AND  sl3.ItemID          = sl.ItemID
              AND  sl3.UnitCost IS NOT NULL
              AND  sl3.UnitCost        > 0
              AND  sl3.TransactionDate <= @AsOfDate
            ORDER BY sl3.TransactionDate DESC, sl3.ID DESC
        ), 0)                                               AS StockValue

    FROM StockLedger               sl
        INNER JOIN WarehouseMaster  wh  ON sl.WarehouseID = wh.ID
        INNER JOIN ItemMaster       im  ON sl.ItemID      = im.ID
        LEFT  JOIN InventoryCategory ic ON im.CategoryID  = ic.ID
        LEFT  JOIN BrandMaster       bm ON im.BrandID     = bm.ID
        LEFT  JOIN UOMMaster         um ON im.UOMID       = um.ID
    WHERE sl.TransactionDate <= @AsOfDate
      AND wh.IsActive = 1
      AND (@WarehouseID = 0 OR sl.WarehouseID = @WarehouseID)
      AND (@CategoryID  = 0 OR im.CategoryID  = @CategoryID)
      AND (@ItemID      = 0 OR sl.ItemID      = @ItemID)
    GROUP BY
        sl.WarehouseID, sl.ItemID,
        wh.ID, wh.Name,
        im.ID, im.Code, im.Name, im.ReorderLevel,
        ic.Name, bm.Name, um.Name
    HAVING
        -- Show only items with positive balance
        (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0)) > 0
    ORDER BY
        wh.Name  ASC,
        ic.Name  ASC,
        im.Name  ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Warehouse Stock Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Warehouse Stock Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 35: sp_WarehouseStockReport and ScreenList entry created successfully.';
GO
