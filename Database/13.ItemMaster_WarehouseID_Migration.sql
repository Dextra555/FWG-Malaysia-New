-- ============================================================
-- Migration: Add WarehouseID to ItemMaster
-- Description: Links each item to a default warehouse
-- Date: 2026-10-06
-- ============================================================

-- Step 1: Add nullable WarehouseID column to ItemMaster
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ItemMaster' AND COLUMN_NAME = 'WarehouseID'
)
BEGIN
    ALTER TABLE ItemMaster
    ADD WarehouseID INT NULL;

    PRINT 'Column WarehouseID added to ItemMaster.';
END
ELSE
BEGIN
    PRINT 'Column WarehouseID already exists in ItemMaster. Skipped.';
END
GO

-- Step 2: Add FK constraint (if not already exists)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_ItemMaster_WarehouseID'
)
BEGIN
    ALTER TABLE ItemMaster
    ADD CONSTRAINT FK_ItemMaster_WarehouseID
        FOREIGN KEY (WarehouseID) REFERENCES WarehouseMaster(ID)
        ON DELETE SET NULL
        ON UPDATE CASCADE;

    PRINT 'FK constraint FK_ItemMaster_WarehouseID added.';
END
ELSE
BEGIN
    PRINT 'FK constraint FK_ItemMaster_WarehouseID already exists. Skipped.';
END
GO

PRINT 'Migration 13.ItemMaster_WarehouseID_Migration completed successfully.';
GO
