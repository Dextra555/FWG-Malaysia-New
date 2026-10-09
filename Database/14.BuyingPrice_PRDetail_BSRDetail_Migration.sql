-- =============================================================================
-- Migration 14: Add BuyingPrice column to PurchaseRequisitionDetail
--               and StockRequestDetail tables
-- Purpose : Auto-fill buying price from ItemMaster into PR and BSR lines.
--           PO and Stock Transfer already had UnitPrice / UnitCost columns;
--           this migration adds the same to PR and BSR so the price is
--           captured at requisition / request stage.
-- Date    : 2026-10-06
-- =============================================================================

-- ── 1. PurchaseRequisitionDetail ─────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME  = 'PurchaseRequisitionDetail'
      AND COLUMN_NAME = 'BuyingPrice'
)
BEGIN
    ALTER TABLE PurchaseRequisitionDetail
        ADD BuyingPrice DECIMAL(18, 4) NULL;

    PRINT 'BuyingPrice column added to PurchaseRequisitionDetail.';
END
ELSE
BEGIN
    PRINT 'BuyingPrice already exists in PurchaseRequisitionDetail — skipped.';
END;

GO

-- ── 2. StockRequestDetail (Branch Stock Request) ──────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME  = 'StockRequestDetail'
      AND COLUMN_NAME = 'BuyingPrice'
)
BEGIN
    ALTER TABLE StockRequestDetail
        ADD BuyingPrice DECIMAL(18, 4) NULL;

    PRINT 'BuyingPrice column added to StockRequestDetail.';
END
ELSE
BEGIN
    PRINT 'BuyingPrice already exists in StockRequestDetail — skipped.';
END;

GO

-- ── 3. Back-fill existing rows from ItemMaster.Price ─────────────────────────
-- Optional: populate BuyingPrice for existing Draft records that have not
-- yet been submitted, so they show the current item price when reopened.
-- Safely skipped if not needed (column stays NULL for historical records).

UPDATE prd
SET    prd.BuyingPrice = im.Price
FROM   PurchaseRequisitionDetail prd
JOIN   ItemMaster im ON im.ID = prd.ItemID
JOIN   PurchaseRequisition pr  ON pr.ID  = prd.PRID
WHERE  prd.BuyingPrice IS NULL
  AND  pr.Status = 'Draft';

PRINT 'Back-filled BuyingPrice for Draft PurchaseRequisitionDetail rows.';

GO

UPDATE srd
SET    srd.BuyingPrice = im.Price
FROM   StockRequestDetail srd
JOIN   ItemMaster im   ON im.ID = srd.ItemID
JOIN   StockRequest sr ON sr.ID = srd.RequestID
WHERE  srd.BuyingPrice IS NULL
  AND  sr.Status = 'Draft';

PRINT 'Back-filled BuyingPrice for Draft StockRequestDetail rows.';

GO

PRINT '=== Migration 14 complete ===';
