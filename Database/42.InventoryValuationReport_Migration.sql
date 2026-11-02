-- ============================================================
-- Migration 42: Inventory Valuation Report
-- Creates sp_InventoryValuationReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report shows the monetary value of inventory on hand
-- as of a given date (snapshot), grouped by Category.
--
-- Logic per item per warehouse (as of @AsOfDate):
--   CurrentStock  = SUM(InQty) - SUM(OutQty) from StockLedger
--   AvgUnitCost   = AVG(UnitCost) from IN transactions (GRN/Opening)
--   ValuationAmt  = CurrentStock × AvgUnitCost
--
-- Filters: Warehouse (optional), Category (optional), AsOfDate
-- Report shows ONLY items with CurrentStock > 0
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_InventoryValuationReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_InventoryValuationReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_InventoryValuationReport
    @WarehouseID INT      = 0,
    @CategoryID  INT      = 0,
    @AsOfDate    DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default AsOfDate to today
    IF @AsOfDate IS NULL
        SET @AsOfDate = CAST(GETDATE() AS DATE);

    -- Extend to end of day (23:59:59)
    SET @AsOfDate = DATEADD(SECOND, 86399, CAST(CAST(@AsOfDate AS DATE) AS DATETIME));

    -- ── Main aggregation ──────────────────────────────────────────────────
    SELECT
        ISNULL(ic.Name, 'Uncategorised')                         AS CategoryName,
        im.ID                                                     AS ItemID,
        im.Code                                                   AS ItemCode,
        im.Name                                                   AS ItemName,
        ISNULL(um.Name, '')                                       AS UOMName,
        ISNULL(wh.Name, '')                                       AS WarehouseName,

        -- Stock movements
        ISNULL(SUM(sl.InQty),  0)                                 AS TotalIn,
        ISNULL(SUM(sl.OutQty), 0)                                 AS TotalOut,
        ISNULL(SUM(sl.InQty),  0) - ISNULL(SUM(sl.OutQty), 0)    AS CurrentStock,

        -- Average unit cost — weighted from all IN transactions with cost
        ISNULL(
            SUM(CASE WHEN sl.InQty > 0 AND sl.UnitCost > 0
                     THEN sl.InQty * sl.UnitCost ELSE 0 END)
            /
            NULLIF(SUM(CASE WHEN sl.InQty > 0 AND sl.UnitCost > 0
                            THEN sl.InQty ELSE 0 END), 0)
        , 0)                                                      AS AvgUnitCost,

        -- Valuation = CurrentStock × AvgUnitCost
        (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0))
        *
        ISNULL(
            SUM(CASE WHEN sl.InQty > 0 AND sl.UnitCost > 0
                     THEN sl.InQty * sl.UnitCost ELSE 0 END)
            /
            NULLIF(SUM(CASE WHEN sl.InQty > 0 AND sl.UnitCost > 0
                            THEN sl.InQty ELSE 0 END), 0)
        , 0)                                                      AS ValuationAmt

    FROM StockLedger              sl
        INNER JOIN ItemMaster      im  ON im.ID = sl.ItemID
        INNER JOIN WarehouseMaster wh  ON wh.ID = sl.WarehouseID
        LEFT  JOIN InventoryCategory ic ON ic.ID = im.CategoryID
        LEFT  JOIN UOMMaster        um  ON um.ID = im.UOMID
    WHERE sl.TransactionDate <= @AsOfDate
      AND (@WarehouseID = 0 OR sl.WarehouseID = @WarehouseID)
      AND (@CategoryID  = 0 OR im.CategoryID  = @CategoryID)
    GROUP BY
        ic.Name,
        im.ID,
        im.Code,
        im.Name,
        um.Name,
        wh.Name
    HAVING
        -- Only items with positive stock balance
        (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0)) > 0
    ORDER BY
        CategoryName ASC,
        im.Name      ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Inventory Valuation Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Inventory Valuation Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 42: sp_InventoryValuationReport and ScreenList entry created successfully.';
GO
