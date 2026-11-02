-- ============================================================
-- Migration 49: Current Stock Report — Add Size column +
--               Category & Size filter parameters
--
-- Changes:
--   1. Refresh sp_CurrentStockReport
--      - Add @CategoryID INT = 0  (0 = All)
--      - Add @Size       NVARCHAR(50) = ''  ('' = All)
--      - Add im.Size to SELECT (display column)
--      - Category filter includes parent + sub-categories
--      - Size filter: when set, excludes NULL-size items
--
-- IDEMPOTENT: DROP + CREATE, safe to re-run.
-- Target DB  : OBMSUATNEW
-- ============================================================

PRINT '============================================================';
PRINT 'Migration 49: Current Stock Report Category/Size - Starting...';
PRINT '============================================================';

IF OBJECT_ID('dbo.sp_CurrentStockReport', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_CurrentStockReport;
    PRINT 'sp_CurrentStockReport: Existing procedure dropped.';
END
GO

CREATE PROCEDURE dbo.sp_CurrentStockReport
    @WarehouseID INT           = 0,
    @ItemID      INT           = 0,
    -- New optional filters
    @CategoryID  INT           = 0,
    @Size        NVARCHAR(50)  = ''
AS
BEGIN
    SET NOCOUNT ON;

    -- Normalize empty string to NULL for cleaner comparison
    IF @Size = '' SET @Size = NULL;

    SELECT
        wh.ID                                   AS WarehouseID,
        wh.Name                                 AS WarehouseName,
        im.ID                                   AS ItemID,
        im.Code                                 AS ItemCode,
        im.Name                                 AS ItemName,
        ISNULL(ic.Name, '')                     AS CategoryName,
        ISNULL(bm.Name, '')                     AS BrandName,
        ISNULL(um.Name, '')                     AS UOMName,
        im.Size                                 AS Size,
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
        INNER JOIN WarehouseMaster   wh ON sl.WarehouseID = wh.ID
        INNER JOIN ItemMaster        im ON sl.ItemID      = im.ID
        LEFT  JOIN InventoryCategory ic ON im.CategoryID  = ic.ID
        LEFT  JOIN BrandMaster       bm ON im.BrandID     = bm.ID
        LEFT  JOIN UOMMaster         um ON im.UOMID       = um.ID

    WHERE wh.IsActive = 1

      -- Warehouse filter (0 = all)
      AND (@WarehouseID = 0 OR sl.WarehouseID = @WarehouseID)

      -- Item filter (0 = all)
      AND (@ItemID = 0 OR sl.ItemID = @ItemID)

      -- Category filter: 0 = all; otherwise selected category + its children
      AND (
          @CategoryID = 0
          OR im.CategoryID = @CategoryID
          OR im.CategoryID IN (
              SELECT ID FROM InventoryCategory
              WHERE ParentCategoryID = @CategoryID
          )
      )

      -- Size filter: NULL = all (incl. items with NULL Size)
      --              value set = exact match only (NULL-size items excluded)
      AND (@Size IS NULL OR im.Size = @Size)

    GROUP BY
        sl.WarehouseID, sl.ItemID,
        wh.ID, wh.Name,
        im.ID, im.Code, im.Name, im.Size, im.ReorderLevel,
        ic.Name, bm.Name, um.Name

    ORDER BY
        wh.Name ASC,
        im.Name ASC;
END
GO

PRINT 'sp_CurrentStockReport: Refreshed with CategoryID, Size parameters and Size column.';

PRINT '';
PRINT '============================================================';
PRINT 'Migration 49: Current Stock Report Category/Size - Completed.';
PRINT '============================================================';

-- ── VERIFICATION ─────────────────────────────────────────────────────────
-- EXEC sp_CurrentStockReport @WarehouseID=0, @ItemID=0;              -- existing call unchanged
-- EXEC sp_CurrentStockReport @CategoryID=5;                          -- category filter
-- EXEC sp_CurrentStockReport @CategoryID=5, @Size='30';              -- category + size
