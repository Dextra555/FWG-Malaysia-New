-- =============================================================
-- Migration 58: Office-specific field on AssetMaster
-- ServiceLastDate — shown when Category AssetType = 'Office'
-- Run once on OBMSUATNEW database
-- =============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AssetMaster' AND COLUMN_NAME = 'ServiceLastDate'
)
BEGIN
    ALTER TABLE [dbo].[AssetMaster]
        ADD [ServiceLastDate] DATE NULL;
    PRINT 'ServiceLastDate column added to AssetMaster.';
END
ELSE
    PRINT 'ServiceLastDate already exists — skipped.';
GO
