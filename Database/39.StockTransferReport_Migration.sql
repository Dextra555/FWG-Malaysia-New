-- ============================================================
-- Migration 39: Stock Transfer Report
-- Creates sp_StockTransferReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report lists all Stock Transfer transactions
-- (warehouse-to-warehouse) filtered by date range,
-- source warehouse, destination warehouse, and item.
-- Each row = one transfer detail line with header info.
--
-- Columns: Transfer No, Transfer Date, Source Warehouse,
--          Dest Warehouse, Item Code, Item Name,
--          Category, UOM, Transfer Qty, Unit Cost, Line Total
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_StockTransferReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_StockTransferReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_StockTransferReport
    @SourceWarehouseID INT      = 0,
    @DestWarehouseID   INT      = 0,
    @ItemID            INT      = 0,
    @FromDate          DATETIME = NULL,
    @ToDate            DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default dates if not provided
    IF @FromDate IS NULL SET @FromDate = DATEADD(DAY, -30, CAST(GETDATE() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(GETDATE() AS DATE);

    -- Extend ToDate to end of day (23:59:59)
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    SELECT
        -- Transfer header
        st.ID                                               AS TransferID,
        st.TransferNo,
        CONVERT(VARCHAR(20), st.TransferDate, 106)          AS TransferDate,
        st.TransferDate                                     AS TransferDateRaw,
        ISNULL(sw.Name, '')                                 AS SourceWarehouseName,
        ISNULL(dw.Name, '')                                 AS DestWarehouseName,
        ISNULL(st.Remarks,  '')                             AS TransferRemarks,
        ISNULL(st.LastUpdatedBy, '')                        AS PreparedBy,

        -- Item detail
        im.Code                                             AS ItemCode,
        im.Name                                             AS ItemName,
        ISNULL(ic.Name, '')                                 AS CategoryName,
        ISNULL(um.Name, '')                                 AS UOMName,

        -- Quantities & cost
        ISNULL(d.TransferQty, 0)                            AS TransferQty,
        ISNULL(d.UnitCost,    0)                            AS UnitCost,
        ISNULL(d.TransferQty, 0) * ISNULL(d.UnitCost, 0)   AS LineTotal,
        ISNULL(d.Remarks,     '')                           AS ItemRemarks

    FROM StockTransfer          st
        INNER JOIN StockTransferDetail d  ON d.TransferID   = st.ID
        INNER JOIN WarehouseMaster     sw ON sw.ID           = st.SourceWarehouseID
        INNER JOIN WarehouseMaster     dw ON dw.ID           = st.DestWarehouseID
        INNER JOIN ItemMaster          im ON im.ID           = d.ItemID
        LEFT  JOIN InventoryCategory   ic ON ic.ID           = im.CategoryID
        LEFT  JOIN UOMMaster           um ON um.ID           = d.UOMID
    WHERE st.IsActive = 1
      AND st.TransferDate BETWEEN @FromDate AND @ToDate
      AND (@SourceWarehouseID = 0 OR st.SourceWarehouseID = @SourceWarehouseID)
      AND (@DestWarehouseID   = 0 OR st.DestWarehouseID   = @DestWarehouseID)
      AND (@ItemID            = 0 OR d.ItemID             = @ItemID)
    ORDER BY
        st.TransferDate   ASC,
        st.TransferNo     ASC,
        im.Name           ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Transfer Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Transfer Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 39: sp_StockTransferReport and ScreenList entry created successfully.';
GO
