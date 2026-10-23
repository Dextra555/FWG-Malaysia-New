-- ============================================================
-- OBMS Migration 29 — Inventory SST (Sales & Service Tax)
--
-- Scope  : Inventory-only.  Purchase workflow: PO → GRN.
--          Finance tables, Finance reports, and Finance code
--          are NOT touched by this migration.
--
-- Design :
--   • SST is per-line on PurchaseOrderDetail (SSTPercent is user-
--     configurable per line: 0 / 6 / 8 or any ad-hoc value).
--   • SSTAmount per line is computed and STORED (audit trail).
--     Formula:  SSTAmount = OrderedQty × UnitPrice × SSTPercent / 100
--     (keeps TotalPrice = OrderedQty × UnitPrice unchanged — ex-SST).
--   • PurchaseOrder header carries SSTTotal + GrandTotal.
--   • GRN detail carries SSTPercent (carried from PO detail) and
--     SSTAmount = ReceivedQty × UnitCost × SSTPercent / 100.
--   • GoodsReceiptNote header carries GRN SSTTotal + GrandTotal.
--   • CreditorInvoice.Total written by SaveGRN now equals GrandTotal
--     (subtotal + SST).  Finance sees the correct payable amount.
--     No Finance code or Finance tables are otherwise changed.
--
-- Idempotent : every ALTER is guarded with IF NOT EXISTS.
--              Existing data is left untouched (defaults to 0).
-- Target DB  : OBMSUATNEW (or production equivalent)
-- ============================================================

SET NOCOUNT ON;
PRINT '============================================================';
PRINT 'Migration 29 — Inventory SST — Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : PurchaseOrderDetail  (per-line SST)
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'PurchaseOrderDetail' AND COLUMN_NAME = 'SSTPercent'
)
BEGIN
    ALTER TABLE PurchaseOrderDetail
        ADD SSTPercent  DECIMAL(5,2)  NOT NULL DEFAULT 0;
    PRINT 'PurchaseOrderDetail: SSTPercent added.';
END
ELSE PRINT 'PurchaseOrderDetail: SSTPercent already exists.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'PurchaseOrderDetail' AND COLUMN_NAME = 'SSTAmount'
)
BEGIN
    ALTER TABLE PurchaseOrderDetail
        ADD SSTAmount   DECIMAL(18,4) NOT NULL DEFAULT 0;
    PRINT 'PurchaseOrderDetail: SSTAmount added.';
END
ELSE PRINT 'PurchaseOrderDetail: SSTAmount already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : PurchaseOrder  (header SST totals)
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'PurchaseOrder' AND COLUMN_NAME = 'SSTTotal'
)
BEGIN
    ALTER TABLE PurchaseOrder
        ADD SSTTotal    DECIMAL(18,4) NOT NULL DEFAULT 0;
    PRINT 'PurchaseOrder: SSTTotal added.';
END
ELSE PRINT 'PurchaseOrder: SSTTotal already exists.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'PurchaseOrder' AND COLUMN_NAME = 'GrandTotal'
)
BEGIN
    ALTER TABLE PurchaseOrder
        ADD GrandTotal  DECIMAL(18,4) NOT NULL DEFAULT 0;
    PRINT 'PurchaseOrder: GrandTotal added.';
END
ELSE PRINT 'PurchaseOrder: GrandTotal already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : GoodsReceiptNoteDetail  (per-line SST)
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'GoodsReceiptNoteDetail' AND COLUMN_NAME = 'SSTPercent'
)
BEGIN
    ALTER TABLE GoodsReceiptNoteDetail
        ADD SSTPercent  DECIMAL(5,2)  NOT NULL DEFAULT 0;
    PRINT 'GoodsReceiptNoteDetail: SSTPercent added.';
END
ELSE PRINT 'GoodsReceiptNoteDetail: SSTPercent already exists.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'GoodsReceiptNoteDetail' AND COLUMN_NAME = 'SSTAmount'
)
BEGIN
    ALTER TABLE GoodsReceiptNoteDetail
        ADD SSTAmount   DECIMAL(18,4) NOT NULL DEFAULT 0;
    PRINT 'GoodsReceiptNoteDetail: SSTAmount added.';
END
ELSE PRINT 'GoodsReceiptNoteDetail: SSTAmount already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 4 : GoodsReceiptNote  (header SST totals)
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'GoodsReceiptNote' AND COLUMN_NAME = 'SSTTotal'
)
BEGIN
    ALTER TABLE GoodsReceiptNote
        ADD SSTTotal    DECIMAL(18,4) NOT NULL DEFAULT 0;
    PRINT 'GoodsReceiptNote: SSTTotal added.';
END
ELSE PRINT 'GoodsReceiptNote: SSTTotal already exists.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'GoodsReceiptNote' AND COLUMN_NAME = 'GrandTotal'
)
BEGIN
    ALTER TABLE GoodsReceiptNote
        ADD GrandTotal  DECIMAL(18,4) NOT NULL DEFAULT 0;
    PRINT 'GoodsReceiptNote: GrandTotal added.';
END
ELSE PRINT 'GoodsReceiptNote: GrandTotal already exists.';
GO


-- ─────────────────────────────────────────────────────────────
-- SECTION 5 : Update sp_Report_PO
--
-- Adds SSTPercent, SSTAmount per line and SSTTotal, GrandTotal
-- to the header result set so POReport.aspx can render them.
-- DROP+CREATE is safe — it is a report-only read SP.
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_Report_PO', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_PO;
GO

CREATE PROCEDURE sp_Report_PO
    @POID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Result Set 1 : Header ────────────────────────────────────────────
    SELECT
        po.ID,
        po.PONumber,
        CONVERT(VARCHAR(20), po.PODate,       106) AS PODate,
        CONVERT(VARCHAR(20), po.ExpectedDate,  106) AS ExpectedDate,
        po.Status,
        ISNULL(sup.Name, '')                       AS SupplierName,
        ISNULL(wh.Name,  '')                       AS WarehouseName,
        ISNULL(pr.PRNumber, '')                    AS LinkedPRNumber,
        ISNULL(po.ApprovedBy,   '')                AS ApprovedBy,
        ISNULL(po.LastUpdatedBy,'')                AS RequestedBy,
        CONVERT(VARCHAR(20), po.ApprovedDate, 106) AS ApprovedDate,
        ISNULL(po.Remarks,      '')                AS Remarks,
        ISNULL(po.RejectReason, '')                AS RejectReason,
        po.TotalAmount,
        po.SSTTotal,
        po.GrandTotal
    FROM  PurchaseOrder        po
    JOIN  Suppliers            sup ON sup.Id   = po.SupplierID
    JOIN  WarehouseMaster      wh  ON wh.ID    = po.WarehouseID
    LEFT  JOIN PurchaseRequisition pr ON pr.ID = po.PRID
    WHERE po.ID = @POID AND po.IsActive = 1;

    -- ── Result Set 2 : Detail lines ──────────────────────────────────────
    SELECT
        d.ID,
        d.POID,
        ISNULL(im.Code,  '')                AS ItemCode,
        ISNULL(im.Name,  '')                AS ItemName,
        ISNULL(ic.Name,  '')                AS CategoryName,
        ISNULL(um.Name,  '')                AS UOMName,
        d.OrderedQty,
        d.ReceivedQty,
        d.OrderedQty - d.ReceivedQty        AS PendingQty,
        d.UnitPrice,
        d.OrderedQty * d.UnitPrice          AS LineTotal,
        d.SSTPercent,
        d.SSTAmount
    FROM  PurchaseOrderDetail  d
    JOIN  ItemMaster           im ON im.ID = d.ItemID
    LEFT  JOIN InventoryCategory ic ON ic.ID = im.CategoryID
    LEFT  JOIN UOMMaster        um ON um.ID  = d.UOMID
    WHERE d.POID = @POID
    ORDER BY d.ID;
END
GO

PRINT 'sp_Report_PO: recreated with SST columns.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 6 : Update sp_Report_GRN
--
-- Adds SSTPercent, SSTAmount per line and SSTTotal, GrandTotal
-- to the header result set so GRNReport.aspx can render them.
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_Report_GRN', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_GRN;
GO

CREATE PROCEDURE sp_Report_GRN
    @GRNID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Result Set 1 : Header ────────────────────────────────────────────
    SELECT
        g.ID,
        g.GRNNumber,
        CONVERT(VARCHAR(20), g.GRNDate, 106)   AS GRNDate,
        po.PONumber,
        ISNULL(sup.Name,   '')                 AS SupplierName,
        ISNULL(wh.Name,    '')                 AS WarehouseName,
        ISNULL(g.LastUpdatedBy, '')            AS ReceivedBy,
        ISNULL(g.DeliveryNoteRef,   '')        AS DeliveryNoteRef,
        ISNULL(g.SupplierInvoiceNo, '')        AS SupplierInvoiceNo,
        ISNULL(g.Remarks,           '')        AS Remarks,
        g.SSTTotal,
        g.GrandTotal
    FROM  GoodsReceiptNote     g
    JOIN  PurchaseOrder        po  ON po.ID  = g.POID
    JOIN  Suppliers            sup ON sup.Id = po.SupplierID
    JOIN  WarehouseMaster      wh  ON wh.ID  = g.WarehouseID
    WHERE g.ID = @GRNID AND g.IsActive = 1;

    -- ── Result Set 2 : Detail lines ──────────────────────────────────────
    SELECT
        d.ID,
        d.GRNID,
        d.PODetailID,
        ISNULL(im.Code, '')                 AS ItemCode,
        ISNULL(im.Name, '')                 AS ItemName,
        ISNULL(ic.Name, '')                 AS CategoryName,
        ISNULL(um.Name, '')                 AS UOMName,
        pd.OrderedQty,
        d.ReceivedQty,
        d.UnitCost,
        d.ReceivedQty * d.UnitCost          AS LineTotal,
        d.SSTPercent,
        d.SSTAmount
    FROM  GoodsReceiptNoteDetail d
    JOIN  PurchaseOrderDetail    pd ON pd.ID  = d.PODetailID
    JOIN  ItemMaster             im ON im.ID  = d.ItemID
    LEFT  JOIN InventoryCategory ic ON ic.ID  = im.CategoryID
    LEFT  JOIN UOMMaster         um ON um.ID  = d.UOMID
    WHERE d.GRNID = @GRNID
    ORDER BY d.ID;
END
GO

PRINT 'sp_Report_GRN: recreated with SST columns.';


-- ─────────────────────────────────────────────────────────────
-- ─────────────────────────────────────────────────────────────
-- SECTION 7 : PurchaseRequisitionDetail  (per-line estimated SST)
--
-- SST on PR is INFORMATIONAL / ESTIMATED only.
-- It has no financial consequence — no CreditorInvoice is created
-- from a PR.  The purpose is to let the purchase team see expected
-- tax at request time and carry it forward to the PO automatically.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'PurchaseRequisitionDetail' AND COLUMN_NAME = 'SSTPercent'
)
BEGIN
    ALTER TABLE PurchaseRequisitionDetail
        ADD SSTPercent  DECIMAL(5,2)  NOT NULL DEFAULT 0;
    PRINT 'PurchaseRequisitionDetail: SSTPercent added.';
END
ELSE PRINT 'PurchaseRequisitionDetail: SSTPercent already exists.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME = 'PurchaseRequisitionDetail' AND COLUMN_NAME = 'SSTAmount'
)
BEGIN
    ALTER TABLE PurchaseRequisitionDetail
        ADD SSTAmount   DECIMAL(18,4) NOT NULL DEFAULT 0;
    PRINT 'PurchaseRequisitionDetail: SSTAmount added.';
END
ELSE PRINT 'PurchaseRequisitionDetail: SSTAmount already exists.';
GO


-- ─────────────────────────────────────────────────────────────
-- SECTION 8 : Update sp_Report_PR
--
-- Adds BuyingPrice, SSTPercent, SSTAmount per line so PRReport.aspx
-- can render estimated costs and SST on the printed requisition.
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_Report_PR', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_PR;
GO

CREATE PROCEDURE sp_Report_PR
    @PRID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Result Set 1 : Header ────────────────────────────────────────────
    SELECT
        pr.ID,
        pr.PRNumber,
        CONVERT(VARCHAR(20), pr.PRDate,         106) AS PRDate,
        CONVERT(VARCHAR(20), pr.RequiredByDate, 106) AS RequiredByDate,
        pr.Status,
        ISNULL(pr.Remarks,          '')              AS Remarks,
        ISNULL(pr.LastUpdatedBy,    '')              AS RequestedBy,
        ISNULL(pr.ApprovedBy,       '')              AS ApprovedBy,
        CONVERT(VARCHAR(20), pr.ApprovedDate,   106) AS ApprovedDate,
        ISNULL(pr.ApprovalRemarks,  '')              AS ApprovalRemarks,
        ISNULL(pr.SubmittedBy, pr.LastUpdatedBy)     AS SubmittedBy
    FROM  PurchaseRequisition pr
    WHERE pr.ID = @PRID AND pr.IsActive = 1;

    -- ── Result Set 2 : Detail lines ──────────────────────────────────────
    SELECT
        d.ID,
        d.PRID,
        ISNULL(im.Code, '')            AS ItemCode,
        ISNULL(im.Name, '')            AS ItemName,
        ISNULL(ic.Name, '')            AS CategoryName,
        ISNULL(um.Name, '')            AS UOMName,
        d.RequiredQty,
        ISNULL(d.BuyingPrice, 0)       AS BuyingPrice,
        d.SSTPercent,
        d.SSTAmount,
        ISNULL(d.Remarks, '')          AS Remarks
    FROM  PurchaseRequisitionDetail d
    JOIN  ItemMaster          im ON im.ID = d.ItemID
    LEFT  JOIN InventoryCategory ic ON ic.ID = im.CategoryID
    LEFT  JOIN UOMMaster       um ON um.ID  = d.UOMID
    WHERE d.PRID = @PRID
    ORDER BY d.ID;
END
GO

PRINT 'sp_Report_PR: recreated with BuyingPrice + SST columns.';



PRINT '============================================================';
PRINT '';
PRINT 'Next steps after running this migration:';
PRINT '  1. Deploy updated OBMSAPP-API (domain models, DTOs, repository)';
PRINT '  2. Deploy updated OBMSAPP-UI  (Angular PO + GRN + PR components)';
PRINT '  3. Deploy updated POReport.aspx, GRNReport.aspx and PRReport.aspx';
PRINT '  4. Recycle IIS Application Pools for both API and Reports sites';
