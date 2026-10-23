-- Migration: Add IsPatrol column to ClientMaster table
-- Date: 2026-10-13

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'IsPatrol'
)
BEGIN
    ALTER TABLE ClientMaster ADD IsPatrol BIT NULL;
    PRINT 'Column IsPatrol added to ClientMaster.';
END
ELSE
BEGIN
    PRINT 'Column IsPatrol already exists in ClientMaster. Skipping.';
END
