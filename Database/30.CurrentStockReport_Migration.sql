-- ============================================================
-- Migration 30: Current Stock Report
-- Creates sp_CurrentStockReport stored procedure
-- and registers the screen in ScreenList for access control.
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_CurrentStockReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CurrentStockReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_CurrentStockReport
    @WarehouseID INT = 0,
    @ItemID      INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        wh.ID                                   AS WarehouseID,
        wh.Name                                 AS WarehouseName,
        im.ID                                   AS ItemID,
        im.Code                                 AS ItemCode,
        im.Name                                 AS ItemName,
        ISNULL(ic.Name, '')                     AS CategoryName,
        ISNULL(bm.Name, '')                     AS BrandName,
        ISNULL(um.Name, '')                     AS UOMName,
        SUM(sl.InQty - sl.OutQty)               AS CurrentStock,
        ISNULL(im.ReorderLevel, 0)              AS ReorderLevel,
        CASE
            WHEN SUM(sl.InQty - sl.OutQty) <= ISNULL(im.ReorderLevel, 0)
            THEN 1 ELSE 0
        END                                     AS BelowReorderLevel,
        (
            SELECT TOP 1 sl2.UnitCost
            FROM StockLedger sl2
            WHERE sl2.WarehouseID = sl.WarehouseID
              AND sl2.ItemID      = sl.ItemID
              AND sl2.UnitCost IS NOT NULL
              AND sl2.UnitCost > 0
            ORDER BY sl2.TransactionDate DESC, sl2.ID DESC
        )                                       AS LastUnitCost,
        SUM(sl.InQty - sl.OutQty) *
        ISNULL(
            (
                SELECT TOP 1 sl3.UnitCost
                FROM StockLedger sl3
                WHERE sl3.WarehouseID = sl.WarehouseID
                  AND sl3.ItemID      = sl.ItemID
                  AND sl3.UnitCost IS NOT NULL
                  AND sl3.UnitCost > 0
                ORDER BY sl3.TransactionDate DESC, sl3.ID DESC
            ), 0
        )                                       AS StockValue
    FROM StockLedger sl
        INNER JOIN WarehouseMaster  wh ON sl.WarehouseID = wh.ID
        INNER JOIN ItemMaster       im ON sl.ItemID      = im.ID
        LEFT  JOIN InventoryCategory ic ON im.CategoryID = ic.ID
        LEFT  JOIN BrandMaster      bm ON im.BrandID     = bm.ID
        LEFT  JOIN UOMMaster        um ON im.UOMID       = um.ID
    WHERE (@WarehouseID = 0 OR sl.WarehouseID = @WarehouseID)
      AND (@ItemID = 0 OR sl.ItemID = @ItemID)
      AND wh.IsActive = 1
    GROUP BY
        sl.WarehouseID, sl.ItemID,
        wh.ID, wh.Name,
        im.ID, im.Code, im.Name, im.ReorderLevel,
        ic.Name, bm.Name, um.Name
    ORDER BY
        wh.Name ASC,
        im.Name ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Current Stock Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Current Stock Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 30: sp_CurrentStockReport and ScreenList entry created successfully.';
GO
