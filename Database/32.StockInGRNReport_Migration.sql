-- ============================================================
-- Migration 32: Stock In / GRN Report
-- Creates sp_StockInGRNReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report lists GRN transactions (Stock In via Goods Receipt)
-- filtered by date range, warehouse, supplier, and item.
-- Each row = one GRN detail line with header info.
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_StockInGRNReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_StockInGRNReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
-- Table column reference:
--   GoodsReceiptNoteDetail : GRNID, PODetailID, ItemID, UOMID, ReceivedQty, UnitCost
--   PurchaseOrderDetail    : ID, POID, ItemID, OrderedQty, UnitPrice
--   (OrderedQty lives in PurchaseOrderDetail, NOT GoodsReceiptNoteDetail)
CREATE PROCEDURE dbo.sp_StockInGRNReport
    @WarehouseID INT      = 0,
    @SupplierID  INT      = 0,
    @ItemID      INT      = 0,
    @FromDate    DATETIME = NULL,
    @ToDate      DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Extend ToDate to end of day (23:59:59)
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    SELECT
        g.ID                                              AS GRNID,
        g.GRNNumber,
        CONVERT(VARCHAR(20), g.GRNDate, 106)              AS GRNDate,
        g.GRNDate                                         AS GRNDateRaw,
        ISNULL(po.PONumber, '')                           AS PONumber,
        ISNULL(sup.Name, '')                              AS SupplierName,
        ISNULL(wh.Name, '')                               AS WarehouseName,
        ISNULL(g.SupplierInvoiceNo, '')                   AS SupplierInvoiceNo,
        ISNULL(g.DeliveryNoteRef, '')                     AS DeliveryNoteRef,
        ISNULL(g.LastUpdatedBy, '')                       AS ReceivedBy,

        -- Item detail
        im.ID                                             AS ItemID,
        im.Code                                           AS ItemCode,
        im.Name                                           AS ItemName,
        ISNULL(ic.Name, '')                               AS CategoryName,
        ISNULL(bm.Name, '')                               AS BrandName,
        ISNULL(um.Name, '')                               AS UOMName,

        -- Quantities & cost
        -- OrderedQty is in PurchaseOrderDetail (joined via d.PODetailID)
        -- GoodsReceiptNoteDetail has: ReceivedQty, UnitCost only
        ISNULL(pod.OrderedQty, 0)                          AS OrderedQty,
        ISNULL(d.ReceivedQty,  0)                          AS ReceivedQty,
        ISNULL(d.UnitCost,     0)                          AS UnitCost,
        ISNULL(d.ReceivedQty,  0) * ISNULL(d.UnitCost, 0)  AS LineTotal,

        -- SST columns added in migration 29
        ISNULL(d.SSTPercent, 0)                            AS SSTPercent,
        ISNULL(d.SSTAmount,  0)                            AS SSTAmount

    FROM GoodsReceiptNote        g
        INNER JOIN GoodsReceiptNoteDetail d   ON d.GRNID     = g.ID
        INNER JOIN PurchaseOrderDetail    pod ON d.PODetailID = pod.ID
        INNER JOIN PurchaseOrder          po  ON g.POID       = po.ID
        INNER JOIN Suppliers              sup ON po.SupplierID = sup.ID
        INNER JOIN WarehouseMaster        wh  ON g.WarehouseID = wh.ID
        INNER JOIN ItemMaster             im  ON d.ItemID      = im.ID
        LEFT  JOIN InventoryCategory      ic  ON im.CategoryID = ic.ID
        LEFT  JOIN BrandMaster            bm  ON im.BrandID    = bm.ID
        LEFT  JOIN UOMMaster              um  ON d.UOMID       = um.ID
    WHERE g.IsActive = 1
      AND g.GRNDate BETWEEN @FromDate AND @ToDate
      AND (@WarehouseID = 0 OR g.WarehouseID = @WarehouseID)
      AND (@SupplierID  = 0 OR po.SupplierID  = @SupplierID)
      AND (@ItemID      = 0 OR d.ItemID       = @ItemID)
    ORDER BY
        g.GRNDate   ASC,
        g.GRNNumber ASC,
        im.Name     ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock In GRN Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock In GRN Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 32: sp_StockInGRNReport and ScreenList entry created successfully.';
GO
