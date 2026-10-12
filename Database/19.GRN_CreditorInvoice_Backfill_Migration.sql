-- ═══════════════════════════════════════════════════════════════════════════
-- Migration 19: GRN → CreditorInvoice Backfill
-- Purpose : Existing GRNs (created before this fix) have no CreditorInvoice
--           row, so they never appeared in vwPaymentDue.
--           This script inserts one CreditorInvoice row per GRN that does
--           not already have one (safe to re-run — IF NOT EXISTS guarded).
--
-- Logic mirrors what SaveGRN() now does automatically going forward:
--   Branch      = WarehouseMaster → BranchMaster.Code
--   Supplier    = GoodsReceiptNote → PurchaseOrder.SupplierID
--   InvoiceNo   = SupplierInvoiceNo (or GRNNumber if blank)
--   InvoiceType = 'G'
--   Total       = SUM(GoodsReceiptNoteDetail.ReceivedQty * UnitCost)
--   PaymentDate = GRNDate
--   RecID       = GoodsReceiptNote.ID   (link back to GRN)
--
-- Run once in SSMS against OBMSUATNEW.
-- Each GO separates batch for SQL Server compiler.
-- ═══════════════════════════════════════════════════════════════════════════

-- ─────────────────────────────────────────────────────────────────────────
-- STEP 1 : Preview — what will be inserted (no data change)
--          Run this SELECT first to verify before executing STEP 2.
-- ─────────────────────────────────────────────────────────────────────────
SELECT
    g.ID                                               AS GRNID,
    g.GRNNumber,
    g.GRNDate,
    po.SupplierID,
    s.Name                                             AS SupplierName,
    bm.Code                                            AS BranchCode,
    ISNULL(NULLIF(LTRIM(RTRIM(g.SupplierInvoiceNo)),
                  ''), g.GRNNumber)                    AS InvoiceNo,
    ISNULL(SUM(d.ReceivedQty * d.UnitCost), 0)        AS GRNTotal,
    g.LastUpdatedBy,
    -- Shows why a GRN would be SKIPPED (already has CreditorInvoice)
    CASE WHEN EXISTS (
            SELECT 1 FROM CreditorInvoice ci
            WHERE ci.InvoiceType = 'G'
              AND ci.RecID       = g.ID
              AND ci.IsDeleted   = 0
         )
         THEN 'SKIP — already exists'
         ELSE 'WILL INSERT'
    END                                                AS Action
FROM GoodsReceiptNote        g
JOIN PurchaseOrder           po ON po.ID = g.POID
JOIN Suppliers               s  ON s.Id  = po.SupplierID
JOIN WarehouseMaster         wm ON wm.ID = g.WarehouseID
JOIN BranchMaster            bm ON bm.ID = wm.BranchID
LEFT JOIN GoodsReceiptNoteDetail d ON d.GRNID = g.ID
WHERE g.IsActive = 1
GROUP BY
    g.ID, g.GRNNumber, g.GRNDate, g.SupplierInvoiceNo,
    g.LastUpdatedBy, po.SupplierID, s.Name, bm.Code
ORDER BY g.ID;
GO

-- ─────────────────────────────────────────────────────────────────────────
-- STEP 2 : Actual INSERT (run after verifying STEP 1 output)
-- ─────────────────────────────────────────────────────────────────────────
INSERT INTO CreditorInvoice
    (Branch, Supplier, RecID, InvoiceNo, InvoiceType,
     InvoiceDate, Total, PaymentDate,
     LASTUPDATE, LastUpdatedBy, ItemCategory, IsDeleted)
SELECT
    bm.Code                                            AS Branch,
    po.SupplierID                                      AS Supplier,
    g.ID                                               AS RecID,
    ISNULL(NULLIF(LTRIM(RTRIM(g.SupplierInvoiceNo)),
                  ''), g.GRNNumber)                    AS InvoiceNo,
    'G'                                                AS InvoiceType,
    g.GRNDate                                          AS InvoiceDate,
    ISNULL(SUM(d.ReceivedQty * d.UnitCost), 0)        AS Total,
    g.GRNDate                                          AS PaymentDate,
    GETDATE()                                          AS LASTUPDATE,
    ISNULL(g.LastUpdatedBy, 'migration')               AS LastUpdatedBy,
    NULL                                               AS ItemCategory,
    0                                                  AS IsDeleted
FROM GoodsReceiptNote        g
JOIN PurchaseOrder           po ON po.ID = g.POID
JOIN WarehouseMaster         wm ON wm.ID = g.WarehouseID
JOIN BranchMaster            bm ON bm.ID = wm.BranchID
LEFT JOIN GoodsReceiptNoteDetail d ON d.GRNID = g.ID
WHERE g.IsActive = 1
  -- Skip GRNs that already have a CreditorInvoice row (safe re-run)
  AND NOT EXISTS (
        SELECT 1 FROM CreditorInvoice ci
        WHERE ci.InvoiceType = 'G'
          AND ci.RecID       = g.ID
          AND ci.IsDeleted   = 0
      )
GROUP BY
    g.ID, g.GRNNumber, g.GRNDate, g.SupplierInvoiceNo,
    g.LastUpdatedBy, po.SupplierID, bm.Code;

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' GRN CreditorInvoice row(s) inserted.';
GO

-- ─────────────────────────────────────────────────────────────────────────
-- STEP 3 : Verify — confirm vwPaymentDue now shows the GRNs
-- ─────────────────────────────────────────────────────────────────────────
SELECT *
FROM   vwPaymentDue
WHERE  InvoiceType = 'G'
ORDER BY ID DESC;
GO
