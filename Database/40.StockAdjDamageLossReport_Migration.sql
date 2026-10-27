-- ============================================================
-- Migration 40: Stock Adjustment / Damage / Loss Report
-- Creates sp_StockAdjDamageLossReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report combines 3 transaction types into one view:
--   1. Stock Adjustment PLUS  (ADJ_PLUS  in StockLedger)
--   2. Stock Adjustment MINUS (ADJ_MINUS in StockLedger)
--   3. Damage / Loss          (DAMAGE_LOSS in StockLedger)
--
-- Source tables: StockAdjustment + StockDamageLoss
-- Filtered by: Warehouse, Transaction Type, Item, Date Range
--
-- Columns: Trans Type, Doc No, Doc Date, Warehouse,
--          Item Code, Item Name, Category, UOM,
--          Qty, Unit Cost, Total Value, Reason
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_StockAdjDamageLossReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_StockAdjDamageLossReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_StockAdjDamageLossReport
    @WarehouseID  INT           = 0,
    @TransType    NVARCHAR(20)  = 'ALL',   -- ALL | ADJ_PLUS | ADJ_MINUS | DAMAGE | LOSS
    @ItemID       INT           = 0,
    @FromDate     DATETIME      = NULL,
    @ToDate       DATETIME      = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default dates if not provided
    IF @FromDate IS NULL SET @FromDate = DATEADD(DAY, -30, CAST(GETDATE() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(GETDATE() AS DATE);

    -- Extend ToDate to end of day (23:59:59)
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    IF @TransType IS NULL OR @TransType = '' SET @TransType = 'ALL';

    -- ── Stock Adjustments ─────────────────────────────────────────────────
    SELECT
        CASE sa.AdjustmentType
            WHEN 'PLUS'  THEN 'Adjustment (+)'
            WHEN 'MINUS' THEN 'Adjustment (-)'
            ELSE sa.AdjustmentType
        END                                                     AS TransactionType,
        sa.AdjustmentType                                       AS TransTypeCode,
        sa.AdjustmentNo                                         AS DocNo,
        CONVERT(VARCHAR(20), sa.AdjustmentDate, 106)            AS DocDate,
        CAST(sa.AdjustmentDate AS DATETIME)                     AS DocDateRaw,
        ISNULL(wh.Name, '')                                     AS WarehouseName,
        im.Code                                                 AS ItemCode,
        im.Name                                                 AS ItemName,
        ISNULL(ic.Name, '')                                     AS CategoryName,
        ISNULL(um.Name, '')                                     AS UOMName,
        ISNULL(sa.AdjustmentQty, 0)                             AS Quantity,
        ISNULL(sa.UnitCost,      0)                             AS UnitCost,
        ISNULL(sa.AdjustmentQty, 0) * ISNULL(sa.UnitCost, 0)   AS TotalValue,
        ISNULL(sa.Reason,  '')                                  AS Reason,
        ISNULL(sa.Remarks, '')                                  AS Remarks,
        ISNULL(sa.LastUpdatedBy, '')                            AS PreparedBy

    FROM StockAdjustment    sa
        INNER JOIN WarehouseMaster   wh ON wh.ID = sa.WarehouseID
        INNER JOIN ItemMaster        im ON im.ID = sa.ItemID
        LEFT  JOIN InventoryCategory ic ON ic.ID = im.CategoryID
        LEFT  JOIN UOMMaster         um ON um.ID = im.UOMID
    WHERE sa.IsActive = 1
      AND CAST(sa.AdjustmentDate AS DATETIME) BETWEEN @FromDate AND @ToDate
      AND (@WarehouseID = 0 OR sa.WarehouseID = @WarehouseID)
      AND (@ItemID      = 0 OR sa.ItemID      = @ItemID)
      AND (
            @TransType = 'ALL'
            OR (@TransType = 'ADJ_PLUS'  AND sa.AdjustmentType = 'PLUS')
            OR (@TransType = 'ADJ_MINUS' AND sa.AdjustmentType = 'MINUS')
          )

    UNION ALL

    -- ── Damage / Loss ─────────────────────────────────────────────────────
    SELECT
        CASE dl.LossType
            WHEN 'Damage' THEN 'Damage'
            WHEN 'Loss'   THEN 'Loss'
            ELSE dl.LossType
        END                                                     AS TransactionType,
        dl.LossType                                             AS TransTypeCode,
        dl.RecordNo                                             AS DocNo,
        CONVERT(VARCHAR(20), dl.RecordDate, 106)                AS DocDate,
        CAST(dl.RecordDate AS DATETIME)                         AS DocDateRaw,
        ISNULL(wh.Name, '')                                     AS WarehouseName,
        im.Code                                                 AS ItemCode,
        im.Name                                                 AS ItemName,
        ISNULL(ic.Name, '')                                     AS CategoryName,
        ISNULL(um.Name, '')                                     AS UOMName,
        ISNULL(dl.Quantity, 0)                                  AS Quantity,
        ISNULL(dl.UnitCost, 0)                                  AS UnitCost,
        ISNULL(dl.Quantity, 0) * ISNULL(dl.UnitCost, 0)        AS TotalValue,
        ISNULL(dl.Reason,  '')                                  AS Reason,
        ISNULL(dl.Remarks, '')                                  AS Remarks,
        ISNULL(dl.LastUpdatedBy, '')                            AS PreparedBy

    FROM StockDamageLoss    dl
        INNER JOIN WarehouseMaster   wh ON wh.ID = dl.WarehouseID
        INNER JOIN ItemMaster        im ON im.ID = dl.ItemID
        LEFT  JOIN InventoryCategory ic ON ic.ID = im.CategoryID
        LEFT  JOIN UOMMaster         um ON um.ID = im.UOMID
    WHERE dl.IsActive = 1
      AND CAST(dl.RecordDate AS DATETIME) BETWEEN @FromDate AND @ToDate
      AND (@WarehouseID = 0 OR dl.WarehouseID = @WarehouseID)
      AND (@ItemID      = 0 OR dl.ItemID      = @ItemID)
      AND (
            @TransType = 'ALL'
            OR (@TransType = 'DAMAGE' AND dl.LossType = 'Damage')
            OR (@TransType = 'LOSS'   AND dl.LossType = 'Loss')
          )

    ORDER BY
        DocDateRaw   ASC,
        DocNo        ASC,
        ItemName     ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Adj Damage Loss Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Adj Damage Loss Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 40: sp_StockAdjDamageLossReport and ScreenList entry created successfully.';
GO
