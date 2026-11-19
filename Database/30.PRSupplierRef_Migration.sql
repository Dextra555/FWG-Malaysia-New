-- ═══════════════════════════════════════════════════════════════════════════
-- Migration 30 : Add SupplierInvoiceNo + DeliveryNoteRef to PurchaseRequisition
--
-- Problem  : C# model PurchaseRequisition has SupplierInvoiceNo and
--            DeliveryNoteRef properties, but the DB table never had
--            these columns added via a migration.
--            EF Core includes ALL mapped columns in every UPDATE statement,
--            so SaveChangesAsync() on a PurchaseRequisition (e.g. during
--            ApprovePR) throws:
--              SqlException: Invalid column name 'SupplierInvoiceNo'
--            This causes ApprovePR to return HTTP 500.
--
-- Fix      : Add the two missing columns (nullable, no default needed).
--
-- Safe to run multiple times: both blocks are guarded by IF NOT EXISTS.
-- ═══════════════════════════════════════════════════════════════════════════

PRINT 'Migration 30 : PurchaseRequisition — adding SupplierInvoiceNo and DeliveryNoteRef';

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME  = 'PurchaseRequisition'
    AND    COLUMN_NAME = 'SupplierInvoiceNo'
)
BEGIN
    ALTER TABLE PurchaseRequisition
        ADD SupplierInvoiceNo NVARCHAR(200) NULL;
    PRINT 'PurchaseRequisition.SupplierInvoiceNo → ADDED';
END
ELSE
    PRINT 'PurchaseRequisition.SupplierInvoiceNo → already exists, skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME  = 'PurchaseRequisition'
    AND    COLUMN_NAME = 'DeliveryNoteRef'
)
BEGIN
    ALTER TABLE PurchaseRequisition
        ADD DeliveryNoteRef NVARCHAR(200) NULL;
    PRINT 'PurchaseRequisition.DeliveryNoteRef → ADDED';
END
ELSE
    PRINT 'PurchaseRequisition.DeliveryNoteRef → already exists, skipped.';
GO

PRINT 'Migration 30 complete. ApprovePR should now work correctly.';
