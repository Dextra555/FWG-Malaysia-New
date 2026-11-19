-- ============================================================
-- Migration 50 — sp_ItemCategoryStockReport : Add Size column
--
-- Purpose : Add im.Size to the SELECT and GROUP BY so that
--           the Item & Category Stock Report shows the item
--           size against each row.
-- Created : 2026-11-03
-- ============================================================

IF OBJECT_ID('dbo.sp_ItemCategoryStockReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ItemCategoryStockReport;
GO

CREATE PROCEDURE dbo.sp_ItemCategoryStockReport
    @WarehouseID INT      = 0,
    @CategoryID  INT      = 0,
    @ItemID      INT      = 0,
    @AsOfDate    DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @AsOfDate IS NULL
        SET @AsOfDate = CAST(GETDATE() AS DATE);

    SET @AsOfDate = DATEADD(SECOND, 86399, CAST(CAST(@AsOfDate AS DATE) AS DATETIME));

    SELECT
        ISNULL(ic.Name,  'Uncategorised')   AS CategoryName,
        im.ID                                AS ItemID,
        im.Code                              AS ItemCode,
        im.Name                              AS ItemName,
        ISNULL(im.Size, '')                  AS Size,
        ISNULL(um.Name, '')                  AS UOMName,
        ISNULL(wh.Name, '')                  AS WarehouseName,

        ISNULL(SUM(sl.InQty),  0)            AS TotalIn,
        ISNULL(SUM(sl.OutQty), 0)            AS TotalOut,

        ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0)
                                             AS ClosingBalance,

        ISNULL(AVG(CASE WHEN sl.UnitCost > 0 THEN sl.UnitCost ELSE NULL END), 0)
                                             AS AvgUnitCost,

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
        im.Size,
        um.Name,
        wh.Name
    HAVING
        (ISNULL(SUM(sl.InQty), 0) - ISNULL(SUM(sl.OutQty), 0)) >= 0
    ORDER BY
        CategoryName ASC,
        im.Name      ASC;
END
GO

PRINT 'Migration 50: sp_ItemCategoryStockReport updated with Size column.';
GO
