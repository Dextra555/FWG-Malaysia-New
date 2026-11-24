-- =============================================================
-- Migration 57: Vehicle-specific fields on AssetMaster
-- RoadTaxRenewalDate + InsuranceRenewalDate
-- Run once on OBMSUATNEW database
-- =============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AssetMaster' AND COLUMN_NAME = 'RoadTaxRenewalDate'
)
BEGIN
    ALTER TABLE [dbo].[AssetMaster]
        ADD [RoadTaxRenewalDate] DATE NULL;
    PRINT 'RoadTaxRenewalDate column added to AssetMaster.';
END
ELSE
    PRINT 'RoadTaxRenewalDate already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AssetMaster' AND COLUMN_NAME = 'InsuranceRenewalDate'
)
BEGIN
    ALTER TABLE [dbo].[AssetMaster]
        ADD [InsuranceRenewalDate] DATE NULL;
    PRINT 'InsuranceRenewalDate column added to AssetMaster.';
END
ELSE
    PRINT 'InsuranceRenewalDate already exists — skipped.';
GO
