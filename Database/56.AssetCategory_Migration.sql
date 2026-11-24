-- =============================================================
-- Migration 56: AssetMaster.CategoryID column
-- Links Asset to existing InventoryCategory table (Cat='P' rows)
-- Run once on OBMSUATNEW database
-- =============================================================

-- Add CategoryID column to AssetMaster (if not exists)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AssetMaster' AND COLUMN_NAME    = 'CategoryID'
)
BEGIN
    ALTER TABLE [dbo].[AssetMaster]
        ADD [CategoryID] INT NULL;

    -- FK to InventoryCategory (Purchase categories, Cat='P')
    ALTER TABLE [dbo].[AssetMaster]
        ADD CONSTRAINT FK_AssetMaster_InventoryCategory
        FOREIGN KEY ([CategoryID]) REFERENCES [dbo].[InventoryCategory]([ID]);

    PRINT 'CategoryID column added to AssetMaster.';
END
ELSE
BEGIN
    PRINT 'CategoryID column already exists on AssetMaster — skipped.';
END
GO
