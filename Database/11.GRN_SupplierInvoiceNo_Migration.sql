-- ═══════════════════════════════════════════════════════════════════════════
-- GRN Supplier Invoice Number Migration
-- Adds SupplierInvoiceNo (NVARCHAR(100) NULL) to GoodsReceiptNote table
-- so the user can record the supplier's own invoice number on a GRN.
-- ═══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1
    FROM   INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME  = 'GoodsReceiptNote'
    AND    COLUMN_NAME = 'SupplierInvoiceNo'
)
BEGIN
    ALTER TABLE GoodsReceiptNote
        ADD SupplierInvoiceNo NVARCHAR(100) NULL;

    PRINT 'Column SupplierInvoiceNo added to GoodsReceiptNote.';
END
ELSE
    PRINT 'Column SupplierInvoiceNo already exists. Skipping.';
GO
