-- ============================================================
-- Migration 60 : AssetMaster.CategoryID FK fix
--
-- Problem : FK_AssetMaster_AssetCategory pointed to 'AssetCategory'
--           table which is empty. UI sends InventoryCategory IDs
--           (1,2,3...) → FK violation → HTTP 500 on SaveAsset.
--
-- Fix     : Drop old FK, add correct FK → InventoryCategory(ID)
--
-- Safe to run multiple times (IF EXISTS guards).
-- Target  : OBMSUATNEW + FWGServerDB
-- ============================================================

PRINT 'Migration 60: AssetMaster CategoryID FK fix';

-- Drop old wrong FK
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_AssetMaster_AssetCategory'
      AND parent_object_id = OBJECT_ID('AssetMaster')
)
BEGIN
    ALTER TABLE AssetMaster DROP CONSTRAINT FK_AssetMaster_AssetCategory;
    PRINT 'FK_AssetMaster_AssetCategory → DROPPED';
END
ELSE
    PRINT 'FK_AssetMaster_AssetCategory → not found, skipping drop';

-- Add correct FK → InventoryCategory
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_AssetMaster_InventoryCategory'
      AND parent_object_id = OBJECT_ID('AssetMaster')
)
BEGIN
    ALTER TABLE AssetMaster ADD CONSTRAINT FK_AssetMaster_InventoryCategory
        FOREIGN KEY (CategoryID) REFERENCES InventoryCategory(ID);
    PRINT 'FK_AssetMaster_InventoryCategory → ADDED';
END
ELSE
    PRINT 'FK_AssetMaster_InventoryCategory → already exists';

PRINT 'Migration 60 complete. SaveAsset should work now.';
