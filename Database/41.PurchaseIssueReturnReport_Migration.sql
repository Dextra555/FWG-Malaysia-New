-- ============================================================
-- Migration 41: Purchase Return & Issue Return Report
-- Creates sp_PurchaseIssueReturnReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report combines 2 return transaction types:
--   1. Purchase Return  — goods returned to supplier after GRN
--   2. Issue Return     — items returned to warehouse after issue
--
-- Source tables:
--   PurchaseReturn + PurchaseReturnDetail + GoodsReceiptNote + Suppliers
--   StockIssueReturn + StockIssueReturnDetail + StockIssueDoc
--
-- Filters: Warehouse, Return Type (ALL|PURCHASE_RETURN|ISSUE_RETURN),
--          Item, Date Range
--
-- Columns: Return Type, Return No, Return Date, Reference Doc,
--          Supplier/Issued To, Warehouse, Item Code, Item Name,
--          Category, UOM, Return Qty, Unit Cost, Total Value, Reason
-- ============================================================

USE [OBMSDB]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_PurchaseIssueReturnReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PurchaseIssueReturnReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_PurchaseIssueReturnReport
    @WarehouseID INT           = 0,
    @ReturnType  NVARCHAR(30)  = 'ALL',   -- ALL | PURCHASE_RETURN | ISSUE_RETURN
    @ItemID      INT           = 0,
    @FromDate    DATETIME      = NULL,
    @ToDate      DATETIME      = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default dates if not provided
    IF @FromDate IS NULL SET @FromDate = DATEADD(DAY, -30, CAST(GETDATE() AS DATE));
    IF @ToDate   IS NULL SET @ToDate   = CAST(GETDATE() AS DATE);

    -- Extend ToDate to end of day (23:59:59)
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    IF @ReturnType IS NULL OR @ReturnType = '' SET @ReturnType = 'ALL';

    -- ── 1. Purchase Returns ───────────────────────────────────────────────
    SELECT
        'Purchase Return'                                       AS ReturnType,
        'PURCHASE_RETURN'                                       AS ReturnTypeCode,
        pr.ReturnNumber                                         AS ReturnNo,
        CONVERT(VARCHAR(20), pr.ReturnDate, 106)                AS ReturnDate,
        CAST(pr.ReturnDate AS DATETIME)                         AS ReturnDateRaw,
        g.GRNNumber                                             AS RefDocNo,
        ISNULL(sup.Name, '')                                    AS PartyName,   -- Supplier
        ISNULL(wh.Name, '')                                     AS WarehouseName,
        im.Code                                                 AS ItemCode,
        im.Name                                                 AS ItemName,
        ISNULL(ic.Name, '')                                     AS CategoryName,
        ISNULL(um.Name, '')                                     AS UOMName,
        ISNULL(prd.ReturnQty, 0)                                AS ReturnQty,
        ISNULL(prd.UnitCost,  0)                                AS UnitCost,
        ISNULL(prd.ReturnQty, 0) * ISNULL(prd.UnitCost, 0)     AS TotalValue,
        ISNULL(pr.Reason, '')                                   AS Reason,
        ISNULL(pr.LastUpdatedBy, '')                            AS PreparedBy

    FROM PurchaseReturn       pr
        INNER JOIN PurchaseReturnDetail  prd ON prd.ReturnID    = pr.ID
        INNER JOIN GoodsReceiptNote      g   ON g.ID            = pr.GRNID
        INNER JOIN PurchaseOrder         po  ON po.ID           = g.POID
        INNER JOIN Suppliers             sup ON sup.ID           = po.SupplierID
        INNER JOIN WarehouseMaster       wh  ON wh.ID            = pr.WarehouseID
        INNER JOIN ItemMaster            im  ON im.ID            = prd.ItemID
        LEFT  JOIN InventoryCategory     ic  ON ic.ID            = im.CategoryID
        LEFT  JOIN UOMMaster             um  ON um.ID            = prd.UOMID
    WHERE pr.IsActive = 1
      AND CAST(pr.ReturnDate AS DATETIME) BETWEEN @FromDate AND @ToDate
      AND (@WarehouseID = 0 OR pr.WarehouseID = @WarehouseID)
      AND (@ItemID      = 0 OR prd.ItemID     = @ItemID)
      AND (@ReturnType IN ('ALL', 'PURCHASE_RETURN'))

    UNION ALL

    -- ── 2. Issue Returns ──────────────────────────────────────────────────
    SELECT
        'Issue Return'                                          AS ReturnType,
        'ISSUE_RETURN'                                          AS ReturnTypeCode,
        sir.ReturnNo,
        CONVERT(VARCHAR(20), sir.ReturnDate, 106)               AS ReturnDate,
        CAST(sir.ReturnDate AS DATETIME)                        AS ReturnDateRaw,
        sid.IssueNo                                             AS RefDocNo,
        ISNULL(sid.IssueToName, sid.IssueToType)                AS PartyName,   -- Issued To
        ISNULL(wh.Name, '')                                     AS WarehouseName,
        im.Code                                                 AS ItemCode,
        im.Name                                                 AS ItemName,
        ISNULL(ic.Name, '')                                     AS CategoryName,
        ISNULL(um.Name, '')                                     AS UOMName,
        ISNULL(sird.ReturnQty, 0)                               AS ReturnQty,
        ISNULL(sird.UnitCost,  0)                               AS UnitCost,
        ISNULL(sird.ReturnQty, 0) * ISNULL(sird.UnitCost, 0)   AS TotalValue,
        ISNULL(sir.Remarks, '')                                 AS Reason,
        ISNULL(sir.LastUpdatedBy, '')                           AS PreparedBy

    FROM StockIssueReturn       sir
        INNER JOIN StockIssueReturnDetail sird ON sird.ReturnID           = sir.ID
        INNER JOIN StockIssueDoc          sid  ON sid.ID                  = sir.OriginalIssueDocID
        INNER JOIN WarehouseMaster        wh   ON wh.ID                   = sir.WarehouseID
        INNER JOIN ItemMaster             im   ON im.ID                   = sird.ItemID
        LEFT  JOIN InventoryCategory      ic   ON ic.ID                   = im.CategoryID
        LEFT  JOIN UOMMaster              um   ON um.ID                   = sird.UOMID
    WHERE sir.IsActive = 1
      AND CAST(sir.ReturnDate AS DATETIME) BETWEEN @FromDate AND @ToDate
      AND (@WarehouseID = 0 OR sir.WarehouseID = @WarehouseID)
      AND (@ItemID      = 0 OR sird.ItemID     = @ItemID)
      AND (@ReturnType IN ('ALL', 'ISSUE_RETURN'))

    ORDER BY
        ReturnDateRaw  ASC,
        ReturnTypeCode ASC,
        ReturnNo       ASC,
        ItemName       ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Purchase Issue Return Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Purchase Issue Return Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 41: sp_PurchaseIssueReturnReport and ScreenList entry created successfully.';
GO
