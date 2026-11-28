-- ============================================================
-- Migration 62: Add BranchName column to AssetMaintenance
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AssetMaintenance' AND COLUMN_NAME = 'BranchName'
)
BEGIN
    ALTER TABLE AssetMaintenance
    ADD BranchName NVARCHAR(100) NULL;
END

-- ============================================================
-- Migration 62b: Make CreditorInvoice.Branch nullable
-- (required for Asset Maintenance payments where branch may be unknown)
-- ============================================================

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'CreditorInvoice'
      AND COLUMN_NAME = 'Branch'
      AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE CreditorInvoice
    ALTER COLUMN Branch VARCHAR(255) NULL;
END
