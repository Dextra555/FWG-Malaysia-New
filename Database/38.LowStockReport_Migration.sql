-- ============================================================
-- Migration 38: Low Stock / Reorder Level Report
-- Creates sp_LowStockReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report shows items whose current stock balance has
-- fallen AT or BELOW the configured ReorderLevel.
-- Useful for purchase planning / replenishment alerts.
--
-- Logic:
--   CurrentStock = SUM(InQty) - SUM(OutQty) from StockLedger
--   Show item if CurrentStock <= ReorderLevel
--   (items with NULL ReorderLevel are excluded — not configured)
--
-- Filters: Warehouse (optional), Category (optional)
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_LowStockReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_LowStockReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_LowStockReport
    @WarehouseID INT = 0,
    @CategoryID  INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ISNULL(ic.Name, 'Uncategorised')                        AS CategoryName,
        im.ID                                                    AS ItemID,
        im.Code                                                  AS ItemCode,
        im.Name                                                  AS ItemName,
        ISNULL(um.Name, '')                                      AS UOMName,
        ISNULL(wh.Name, '')                                      AS WarehouseName,

        -- Current stock balance per warehouse
        ISNULL(SUM(sl.InQty),  0) - ISNULL(SUM(sl.OutQty), 0)  AS CurrentStock,

        -- Reorder level set on the item
        ISNULL(im.ReorderLevel,    0)                            AS ReorderLevel,

        -- Reorder quantity (suggested purchase qty)
        ISNULL(im.ReorderQuantity, 0)                            AS ReorderQuantity,

        -- Shortage = how much more needed to reach reorder qty above level
        -- e.g. ReorderLevel=10, CurrentStock=3, ReorderQty=50 → Shortage=47
        CASE
            WHEN ISNULL(im.ReorderQuantity, 0) > 0
            THEN ISNULL(im.ReorderQuantity, 0)
                 - (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0))
            ELSE ISNULL(im.ReorderLevel, 0)
                 - (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0))
        END                                                      AS ShortageQty,

        -- Status label
        CASE
            WHEN (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0)) <= 0
                 THEN 'Out of Stock'
            ELSE 'Low Stock'
        END                                                      AS StockStatus,

        -- Average unit cost for value estimate
        ISNULL(AVG(CASE WHEN sl.UnitCost > 0
                        THEN sl.UnitCost ELSE NULL END), 0)      AS AvgUnitCost

    FROM StockLedger              sl
        INNER JOIN ItemMaster     im  ON sl.ItemID      = im.ID
        INNER JOIN WarehouseMaster wh  ON sl.WarehouseID = wh.ID
        LEFT  JOIN InventoryCategory ic ON im.CategoryID = ic.ID
        LEFT  JOIN UOMMaster       um  ON im.UOMID      = um.ID
    WHERE
        -- Only items that have a ReorderLevel configured
        im.ReorderLevel IS NOT NULL
        AND im.ReorderLevel > 0
        AND (@WarehouseID = 0 OR sl.WarehouseID = @WarehouseID)
        AND (@CategoryID  = 0 OR im.CategoryID  = @CategoryID)
    GROUP BY
        ic.Name,
        im.ID,
        im.Code,
        im.Name,
        um.Name,
        wh.Name,
        im.ReorderLevel,
        im.ReorderQuantity
    HAVING
        -- Only show items at or below reorder level
        (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0))
        <= im.ReorderLevel
    ORDER BY
        -- Out of Stock first, then lowest stock first
        StockStatus      ASC,
        CurrentStock     ASC,
        CategoryName     ASC,
        im.Name          ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Low Stock Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Low Stock Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 38: sp_LowStockReport and ScreenList entry created successfully.';
GO
