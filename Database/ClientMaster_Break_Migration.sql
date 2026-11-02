-- Migration: Add Break1 to Break5 columns to ClientMaster table
-- Date: 2026-10-21
-- Description: Stores break durations (in hours) per client. Values: 0, 0.5, 1, 1.5, 2 (NULL means not set)

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break1'
)
BEGIN
    ALTER TABLE ClientMaster ADD Break1 DECIMAL(3,1) NULL;
    PRINT 'Column Break1 added to ClientMaster.';
END
ELSE
BEGIN
    PRINT 'Column Break1 already exists in ClientMaster. Skipping.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break2'
)
BEGIN
    ALTER TABLE ClientMaster ADD Break2 DECIMAL(3,1) NULL;
    PRINT 'Column Break2 added to ClientMaster.';
END
ELSE
BEGIN
    PRINT 'Column Break2 already exists in ClientMaster. Skipping.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break3'
)
BEGIN
    ALTER TABLE ClientMaster ADD Break3 DECIMAL(3,1) NULL;
    PRINT 'Column Break3 added to ClientMaster.';
END
ELSE
BEGIN
    PRINT 'Column Break3 already exists in ClientMaster. Skipping.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break4'
)
BEGIN
    ALTER TABLE ClientMaster ADD Break4 DECIMAL(3,1) NULL;
    PRINT 'Column Break4 added to ClientMaster.';
END
ELSE
BEGIN
    PRINT 'Column Break4 already exists in ClientMaster. Skipping.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break5'
)
BEGIN
    ALTER TABLE ClientMaster ADD Break5 DECIMAL(3,1) NULL;
    PRINT 'Column Break5 added to ClientMaster.';
END
ELSE
BEGIN
    PRINT 'Column Break5 already exists in ClientMaster. Skipping.';
END
