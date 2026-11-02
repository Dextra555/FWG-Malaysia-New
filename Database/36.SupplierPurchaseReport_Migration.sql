-- ============================================================
-- Migration 36: Supplier-wise Purchase Report
-- Creates sp_SupplierPurchaseReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report summarises all GRN purchases grouped by Supplier,
-- filtered by date range, warehouse, supplier, and item.
-- Each row = one GRN detail line with supplier & header info.
--
-- Columns: GRN No, GRN Date, PO No, Warehouse,
--          Item Code, Item Name, Category, UOM,
--          Ordered Qty, Received Qty, Unit Cost, SST%, SST Amt, Line Total
--
-- Grouped in PDF by Supplier with sub-total per supplier.
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_SupplierPurchaseReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SupplierPurchaseReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_SupplierPurchaseReport
    @WarehouseID INT      = 0,
    @SupplierID  INT      = 0,
    @ItemID      INT      = 0,
    @FromDate    DATETIME = NULL,
    @ToDate      DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default dates if not provided
    IF @FromDate IS NULL SET @FromDate = DATEADD(DAY, -30, CAST(GETDATE() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(GETDATE() AS DATE);

    -- Extend ToDate to end of day (23:59:59)
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    SELECT
        -- Supplier info (for grouping)
        ISNULL(sup.Name, '')                              AS SupplierName,
        ISNULL(sup.Code, '')                              AS SupplierCode,
        ISNULL(sup.Phone,'')                              AS SupplierPhone,

        -- GRN header
        g.ID                                              AS GRNID,
        g.GRNNumber,
        CONVERT(VARCHAR(20), g.GRNDate, 106)              AS GRNDate,
        g.GRNDate                                         AS GRNDateRaw,
        ISNULL(po.PONumber, '')                           AS PONumber,
        ISNULL(g.SupplierInvoiceNo, '')                   AS SupplierInvoiceNo,
        ISNULL(wh.Name, '')                               AS WarehouseName,

        -- Item detail
        im.Code                                           AS ItemCode,
        im.Name                                           AS ItemName,
        ISNULL(ic.Name, '')                               AS CategoryName,
        ISNULL(um.Name, '')                               AS UOMName,

        -- Quantities & cost
        ISNULL(pod.OrderedQty,  0)                        AS OrderedQty,
        ISNULL(d.ReceivedQty,   0)                        AS ReceivedQty,
        ISNULL(d.UnitCost,      0)                        AS UnitCost,
        ISNULL(d.SSTPercent,    0)                        AS SSTPercent,
        ISNULL(d.SSTAmount,     0)                        AS SSTAmount,
        ISNULL(d.ReceivedQty,   0) * ISNULL(d.UnitCost, 0) AS LineTotal

    FROM GoodsReceiptNote        g
        INNER JOIN GoodsReceiptNoteDetail d   ON d.GRNID      = g.ID
        INNER JOIN PurchaseOrderDetail    pod ON d.PODetailID  = pod.ID
        INNER JOIN PurchaseOrder          po  ON g.POID        = po.ID
        INNER JOIN Suppliers              sup ON po.SupplierID = sup.ID
        INNER JOIN WarehouseMaster        wh  ON g.WarehouseID = wh.ID
        INNER JOIN ItemMaster             im  ON d.ItemID      = im.ID
        LEFT  JOIN InventoryCategory      ic  ON im.CategoryID = ic.ID
        LEFT  JOIN UOMMaster              um  ON d.UOMID       = um.ID
    WHERE g.IsActive = 1
      AND g.GRNDate BETWEEN @FromDate AND @ToDate
      AND (@WarehouseID = 0 OR g.WarehouseID  = @WarehouseID)
      AND (@SupplierID  = 0 OR po.SupplierID  = @SupplierID)
      AND (@ItemID      = 0 OR d.ItemID       = @ItemID)
    ORDER BY
        sup.Name    ASC,
        g.GRNDate   ASC,
        g.GRNNumber ASC,
        im.Name     ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Supplier Purchase Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Supplier Purchase Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 36: sp_SupplierPurchaseReport and ScreenList entry created successfully.';
GO
