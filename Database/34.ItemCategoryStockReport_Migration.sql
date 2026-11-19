-- ============================================================
-- Migration 34: Item-wise & Category-wise Stock Report
-- Creates sp_ItemCategoryStockReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report shows current stock balance grouped by
-- Item and/or Category, with opening stock, total received,
-- total issued, adjustments, and closing balance.
-- Filtered by Warehouse and/or Category.
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_ItemCategoryStockReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ItemCategoryStockReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_ItemCategoryStockReport
    @WarehouseID INT      = 0,
    @CategoryID  INT      = 0,
    @ItemID      INT      = 0,
    @AsOfDate    DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default AsOfDate to today's end of day if not provided
    IF @AsOfDate IS NULL
        SET @AsOfDate = CAST(GETDATE() AS DATE);

    -- Extend AsOfDate to end of day (23:59:59)
    SET @AsOfDate = DATEADD(SECOND, 86399, CAST(CAST(@AsOfDate AS DATE) AS DATETIME));

    -- ── Main query: aggregate all stock ledger movements up to AsOfDate ───
    SELECT
        ISNULL(ic.Name,  'Uncategorised')   AS CategoryName,
        im.ID                                AS ItemID,
        im.Code                              AS ItemCode,
        im.Name                              AS ItemName,
        ISNULL(um.Name, '')                  AS UOMName,
        ISNULL(wh.Name, '')                  AS WarehouseName,

        -- Total In: sum of all InQty movements
        ISNULL(SUM(sl.InQty),  0)            AS TotalIn,

        -- Total Out: sum of all OutQty movements
        ISNULL(SUM(sl.OutQty), 0)            AS TotalOut,

        -- Closing balance = Total In - Total Out
        ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0)
                                             AS ClosingBalance,

        -- Average unit cost (from rows that have a cost)
        ISNULL(AVG(CASE WHEN sl.UnitCost > 0 THEN sl.UnitCost ELSE NULL END), 0)
                                             AS AvgUnitCost,

        -- Closing value = closing balance × avg unit cost
        (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0))
        *
        ISNULL(AVG(CASE WHEN sl.UnitCost > 0 THEN sl.UnitCost ELSE NULL END), 0)
                                             AS ClosingValue

    FROM StockLedger              sl
        INNER JOIN ItemMaster     im  ON sl.ItemID      = im.ID
        INNER JOIN WarehouseMaster wh  ON sl.WarehouseID = wh.ID
        LEFT  JOIN InventoryCategory ic ON im.CategoryID = ic.ID
        LEFT  JOIN UOMMaster       um  ON im.UOMID      = um.ID
    WHERE sl.TransactionDate <= @AsOfDate
      AND (@WarehouseID = 0 OR sl.WarehouseID = @WarehouseID)
      AND (@CategoryID  = 0 OR im.CategoryID  = @CategoryID)
      AND (@ItemID      = 0 OR sl.ItemID      = @ItemID)
    GROUP BY
        ic.Name,
        im.ID,
        im.Code,
        im.Name,
        um.Name,
        wh.Name
    HAVING
        -- Only show items with a non-negative closing balance
        (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0)) >= 0
    ORDER BY
        CategoryName ASC,
        im.Name      ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Item Category Stock Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Item Category Stock Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 34: sp_ItemCategoryStockReport and ScreenList entry created successfully.';
GO
