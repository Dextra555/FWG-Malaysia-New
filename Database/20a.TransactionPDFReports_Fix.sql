-- ============================================================
-- OBMS Migration 20a — Fix sp_Report_PO and sp_Report_GRN
-- Corrects invalid column references from Migration 20:
--   Suppliers.CompanyName → Suppliers.Name
--   Suppliers.Address     → Suppliers.Address1 + Address2
--   Suppliers.Email       → (removed — column does not exist)
--
-- Run this against OBMSUATNEW after running Migration 20.
-- Safe to re-run (DROP + CREATE).
-- ============================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT 'Fixing sp_Report_PO ...';
GO

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
        -- Build a single address string from the two address columns
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

PRINT 'sp_Report_PO fixed.';
GO

-- ─────────────────────────────────────────────────────────────

PRINT 'Fixing sp_Report_GRN ...';
GO

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

PRINT 'sp_Report_GRN fixed.';
GO

PRINT 'Migration 20a complete — sp_Report_PO and sp_Report_GRN are now correct.';
