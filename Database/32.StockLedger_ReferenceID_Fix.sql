-- ============================================================
-- Migration 32: Update sp_InventoryStockLedger to include
--               ReferenceID in result set (needed for 
--               Opening Stock hyperlink in PDF report).
-- ============================================================

PRINT '============================================================';
PRINT 'Migration 32: sp_InventoryStockLedger - Add ReferenceID';
PRINT '============================================================';

IF OBJECT_ID('sp_InventoryStockLedger', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE sp_InventoryStockLedger;
    PRINT 'sp_InventoryStockLedger: Existing procedure dropped.';
END
GO

CREATE PROCEDURE sp_InventoryStockLedger
    @WarehouseID     INT            = 0,
    @ItemID          INT            = 0,
    @TransactionType NVARCHAR(50)   = 'ALL',
    @FromDate        DATETIME       = NULL,
    @ToDate          DATETIME       = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default date range: current month if not supplied
    IF @FromDate IS NULL
        SET @FromDate = DATEADD(DAY, 1 - DAY(GETDATE()), CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL
        SET @ToDate = GETDATE();

    -- Extend ToDate to end of day so full day is included
    SET @ToDate = CAST(@ToDate AS DATE);
    SET @ToDate = DATEADD(SECOND, 86399, CAST(@ToDate AS DATETIME)); -- 23:59:59

    SELECT
        v.ID,
        v.TransactionNo,
        v.TransactionDate,
        v.TransactionType,
        v.TransactionTypeLabel,
        v.WarehouseID,
        v.WarehouseName,
        v.ItemID,
        v.ItemCode,
        v.ItemName,
        v.CategoryName,
        v.BrandName,
        v.UOMName,
        v.InQty,
        v.OutQty,
        v.RunningBalance,
        v.UnitCost,
        v.ReferenceType,
        v.ReferenceID,
        v.ReferenceNo,
        v.Remarks
    FROM VWStockLedger v
    WHERE
        -- Date range filter (always applied)
        v.TransactionDate >= @FromDate
        AND v.TransactionDate <= @ToDate

        -- Warehouse filter (0 = all)
        AND (@WarehouseID = 0 OR v.WarehouseID = @WarehouseID)

        -- Item filter (0 = all)
        AND (@ItemID = 0 OR v.ItemID = @ItemID)

        -- TransactionType filter ('ALL' = all types)
        AND (@TransactionType = 'ALL' OR v.TransactionType = @TransactionType)

    ORDER BY
        v.ItemName        ASC,
        v.TransactionDate ASC,
        v.ID              ASC;
END
GO

PRINT 'sp_InventoryStockLedger: Updated successfully (ReferenceID added).';
PRINT '';
PRINT '============================================================';
PRINT 'Migration 32 Complete.';
PRINT '============================================================';
