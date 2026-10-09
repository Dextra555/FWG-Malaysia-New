-- ═══════════════════════════════════════════════════════════════════════════
-- GRN → Finance Payment Link Migration
-- Adds GRNID (nullable FK) to BranchPaymentDetails so a payment detail row
-- can reference a GoodsReceiptNote instead of (or in addition to) a
-- CreditorInvoice.  No existing data or logic is changed.
--
-- Run this script in SSMS or sqlcmd.
-- Each section is separated by GO so SQL Server sees the committed schema
-- before the next DDL statement is compiled.
-- ═══════════════════════════════════════════════════════════════════════════

-- ─────────────────────────────────────────────────────────────────────────
-- STEP 1 : Add GRNID column
-- ─────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1
    FROM   INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME  = 'BranchPaymentDetails'
    AND    COLUMN_NAME = 'GRNID'
)
BEGIN
    ALTER TABLE BranchPaymentDetails
        ADD GRNID INT NULL;

    PRINT 'Column GRNID added to BranchPaymentDetails.';
END
ELSE
    PRINT 'Column GRNID already exists in BranchPaymentDetails. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────────────────
-- STEP 2 : Add FK constraint → GoodsReceiptNote(ID)
-- ─────────────────────────────────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GoodsReceiptNote')
   AND NOT EXISTS (
       SELECT 1 FROM sys.foreign_keys
       WHERE  name = 'FK_BranchPaymentDetails_GRN'
   )
BEGIN
    ALTER TABLE BranchPaymentDetails
        ADD CONSTRAINT FK_BranchPaymentDetails_GRN
            FOREIGN KEY (GRNID) REFERENCES GoodsReceiptNote(ID);

    PRINT 'FK_BranchPaymentDetails_GRN created.';
END
ELSE
    PRINT 'FK_BranchPaymentDetails_GRN already exists or GoodsReceiptNote table not found. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────────────────
-- STEP 3 : Filtered index for join performance
-- A filtered index with WHERE GRNID IS NOT NULL must be created in a
-- separate batch (after GO) so the column is fully visible to the compiler.
-- ─────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1
    FROM   sys.indexes
    WHERE  name      = 'IX_BranchPaymentDetails_GRNID'
    AND    object_id = OBJECT_ID('BranchPaymentDetails')
)
BEGIN
    CREATE INDEX IX_BranchPaymentDetails_GRNID
        ON BranchPaymentDetails(GRNID)
        WHERE GRNID IS NOT NULL;

    PRINT 'Index IX_BranchPaymentDetails_GRNID created.';
END
ELSE
    PRINT 'Index IX_BranchPaymentDetails_GRNID already exists. Skipping.';
GO
