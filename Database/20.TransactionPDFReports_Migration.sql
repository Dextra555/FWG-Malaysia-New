-- ============================================================
-- OBMS Migration 20 — Transaction PDF Report Stored Procedures
-- Creates 4 SPs for PR / PO / GRN / BSR PDF reports.
--
-- Each SP accepts a single document ID and returns:
--   Result Set 1 : document header row
--   Result Set 2 : document detail / line rows
--
-- This script is IDEMPOTENT — safe to re-run.
-- ============================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '============================================================';
PRINT 'Migration 20 — Transaction PDF Report SPs — Starting...';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- SP 1 : sp_Report_PR
-- Returns header (PurchaseRequisition) + detail rows
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_Report_PR', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_PR;
GO

CREATE PROCEDURE sp_Report_PR
    @PRID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Header ──────────────────────────────────────────────────────────────
    SELECT
        pr.ID,
        pr.PRNumber,
        CONVERT(VARCHAR(20), pr.PRDate,        106) AS PRDate,
        CONVERT(VARCHAR(20), pr.RequiredByDate, 106) AS RequiredByDate,
        pr.Status,
        ISNULL(pr.Remarks, '')          AS Remarks,
        ISNULL(pr.LastUpdatedBy, '')    AS RequestedBy,
        ISNULL(pr.ApprovedBy, '')       AS ApprovedBy,
        CONVERT(VARCHAR(20), pr.ApprovedDate, 106) AS ApprovedDate,
        ISNULL(pr.ApprovalRemarks, '')  AS ApprovalRemarks,
        ISNULL(pr.SubmittedBy, pr.LastUpdatedBy) AS SubmittedBy
    FROM PurchaseRequisition pr
    WHERE pr.ID = @PRID
      AND pr.IsActive = 1;

    -- ── Detail lines ─────────────────────────────────────────────────────────
    SELECT
        d.ID,
        d.PRID,
        im.Code                         AS ItemCode,
        im.Name                         AS ItemName,
        ISNULL(ic.Name, '')             AS CategoryName,
        ISNULL(um.Name, '')             AS UOMName,
        d.RequiredQty,
        ISNULL(d.BuyingPrice, 0)        AS BuyingPrice,
        ISNULL(d.Remarks, '')           AS Remarks
    FROM PurchaseRequisitionDetail d
    INNER JOIN ItemMaster          im ON im.ID = d.ItemID
    LEFT  JOIN InventoryCategory   ic ON ic.ID = im.CategoryID
    LEFT  JOIN UOMMaster           um ON um.ID = d.UOMID
    WHERE d.PRID = @PRID
    ORDER BY d.ID;
END
GO

PRINT 'sp_Report_PR created.';


-- ─────────────────────────────────────────────────────────────
-- SP 2 : sp_Report_PO
-- Returns header (PurchaseOrder + Supplier + Warehouse + linked PR)
-- + detail rows
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_Report_PO', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_PO;
GO

CREATE PROCEDURE sp_Report_PO
    @POID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Header ──────────────────────────────────────────────────────────────
    SELECT
        po.ID,
        po.PONumber,
        CONVERT(VARCHAR(20), po.PODate,       106) AS PODate,
        CONVERT(VARCHAR(20), po.ExpectedDate, 106) AS ExpectedDate,
        po.Status,
        ISNULL(po.Remarks, '')              AS Remarks,
        ISNULL(po.TotalAmount, 0)           AS TotalAmount,
        s.Name                              AS SupplierName,
        ISNULL(NULLIF(RTRIM(ISNULL(s.Address1,'') + ' ' + ISNULL(s.Address2,'')), ''), '') AS SupplierAddress,
        ISNULL(s.Phone, '')                 AS SupplierPhone,
        wm.Name                             AS WarehouseName,
        ISNULL(pr.PRNumber, '')             AS LinkedPRNumber,
        po.PRID,
        ISNULL(po.LastUpdatedBy, '')        AS RequestedBy,
        ISNULL(po.SubmittedBy, po.LastUpdatedBy) AS SubmittedBy,
        ISNULL(po.ApprovedBy, '')           AS ApprovedBy,
        CONVERT(VARCHAR(20), po.ApprovedDate, 106) AS ApprovedDate,
        ISNULL(po.RejectReason, '')         AS RejectReason
    FROM PurchaseOrder po
    INNER JOIN Suppliers          s  ON s.Id  = po.SupplierID
    INNER JOIN WarehouseMaster    wm ON wm.ID = po.WarehouseID
    LEFT  JOIN PurchaseRequisition pr ON pr.ID = po.PRID
    WHERE po.ID = @POID
      AND po.IsActive = 1;

    -- ── Detail lines ─────────────────────────────────────────────────────────
    SELECT
        d.ID,
        d.POID,
        im.Code                         AS ItemCode,
        im.Name                         AS ItemName,
        ISNULL(ic.Name, '')             AS CategoryName,
        ISNULL(um.Name, '')             AS UOMName,
        d.OrderedQty,
        ISNULL(d.ReceivedQty, 0)        AS ReceivedQty,
        (d.OrderedQty - ISNULL(d.ReceivedQty, 0)) AS PendingQty,
        d.UnitPrice,
        d.TotalPrice                    AS LineTotal,
        ISNULL(d.Remarks, '')           AS Remarks
    FROM PurchaseOrderDetail d
    INNER JOIN ItemMaster          im ON im.ID = d.ItemID
    LEFT  JOIN InventoryCategory   ic ON ic.ID = im.CategoryID
    LEFT  JOIN UOMMaster           um ON um.ID = d.UOMID
    WHERE d.POID = @POID
    ORDER BY d.ID;
END
GO

PRINT 'sp_Report_PO created.';


-- ─────────────────────────────────────────────────────────────
-- SP 3 : sp_Report_GRN
-- Returns header (GoodsReceiptNote + PO + Supplier + Warehouse)
-- + detail rows
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_Report_GRN', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_GRN;
GO

CREATE PROCEDURE sp_Report_GRN
    @GRNID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Header ──────────────────────────────────────────────────────────────
    SELECT
        g.ID,
        g.GRNNumber,
        CONVERT(VARCHAR(20), g.GRNDate, 106) AS GRNDate,
        po.PONumber,
        s.Name                              AS SupplierName,
        wm.Name                             AS WarehouseName,
        ISNULL(g.DeliveryNoteRef, '')       AS DeliveryNoteRef,
        ISNULL(g.SupplierInvoiceNo, '')     AS SupplierInvoiceNo,
        ISNULL(g.Remarks, '')               AS Remarks,
        ISNULL(g.LastUpdatedBy, '')         AS ReceivedBy
    FROM GoodsReceiptNote g
    INNER JOIN PurchaseOrder   po ON po.ID = g.POID
    INNER JOIN Suppliers        s ON s.Id  = po.SupplierID
    INNER JOIN WarehouseMaster wm ON wm.ID = g.WarehouseID
    WHERE g.ID = @GRNID
      AND g.IsActive = 1;

    -- ── Detail lines ─────────────────────────────────────────────────────────
    SELECT
        d.ID,
        d.GRNID,
        im.Code                             AS ItemCode,
        im.Name                             AS ItemName,
        ISNULL(ic.Name, '')                 AS CategoryName,
        ISNULL(um.Name, '')                 AS UOMName,
        pod.OrderedQty,
        d.ReceivedQty,
        d.UnitCost,
        (d.ReceivedQty * d.UnitCost)        AS LineTotal,
        ISNULL(pod.Remarks, '')             AS Remarks
    FROM GoodsReceiptNoteDetail d
    INNER JOIN ItemMaster          im  ON im.ID  = d.ItemID
    INNER JOIN PurchaseOrderDetail pod ON pod.ID = d.PODetailID
    LEFT  JOIN InventoryCategory   ic  ON ic.ID  = im.CategoryID
    LEFT  JOIN UOMMaster           um  ON um.ID  = d.UOMID
    WHERE d.GRNID = @GRNID
    ORDER BY d.ID;
END
GO

PRINT 'sp_Report_GRN created.';


-- ─────────────────────────────────────────────────────────────
-- SP 4 : sp_Report_BSR
-- Returns header (StockRequest + Branch + Warehouse)
-- + detail rows
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_Report_BSR', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_BSR;
GO

CREATE PROCEDURE sp_Report_BSR
    @BSRID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Header ──────────────────────────────────────────────────────────────
    SELECT
        sr.ID,
        sr.RequestNo,
        CONVERT(VARCHAR(20), sr.RequestDate, 106) AS RequestDate,
        sr.Status,
        ISNULL(sr.Remarks, '')              AS Remarks,
        sr.RequestedBy,
        ISNULL(sr.SubmittedBy, sr.LastUpdatedBy) AS SubmittedBy,
        ISNULL(sr.ApprovedBy, '')           AS ApprovedBy,
        CONVERT(VARCHAR(20), sr.ApprovedDate, 106) AS ApprovedDate,
        ISNULL(sr.ApprovalRemarks, '')      AS ApprovalRemarks,
        ISNULL(bm.Name, CAST(sr.RequestingBranchID AS VARCHAR)) AS BranchName,
        wm.Name                             AS WarehouseName,
        ISNULL(st.TransferNo, '')           AS LinkedTransferNo
    FROM StockRequest sr
    INNER JOIN WarehouseMaster wm ON wm.ID = sr.SourceWarehouseID
    LEFT  JOIN BranchMaster    bm ON bm.ID = sr.RequestingBranchID
    LEFT  JOIN StockTransfer   st ON st.ID = sr.LinkedTransferID
    WHERE sr.ID = @BSRID
      AND sr.IsActive = 1;

    -- ── Detail lines ─────────────────────────────────────────────────────────
    SELECT
        d.ID,
        d.RequestID,
        im.Code                             AS ItemCode,
        im.Name                             AS ItemName,
        ISNULL(ic.Name, '')                 AS CategoryName,
        ISNULL(um.Name, '')                 AS UOMName,
        d.RequestedQty,
        ISNULL(d.BuyingPrice, 0)            AS BuyingPrice,
        ISNULL(cm.Name, '')                 AS ClientName,
        ISNULL(d.Remarks, '')               AS Remarks
    FROM StockRequestDetail d
    INNER JOIN ItemMaster          im ON im.ID = d.ItemID
    LEFT  JOIN InventoryCategory   ic ON ic.ID = im.CategoryID
    LEFT  JOIN UOMMaster           um ON um.ID = d.UOMID
    LEFT  JOIN ClientMaster        cm ON cm.ID = d.ClientID
    WHERE d.RequestID = @BSRID
    ORDER BY d.ID;
END
GO

PRINT 'sp_Report_BSR created.';

PRINT '============================================================';
PRINT 'Migration 20 — All 4 report SPs created successfully.';
PRINT '============================================================';
