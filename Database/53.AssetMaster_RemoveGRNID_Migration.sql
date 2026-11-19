-- ============================================================
-- OBMS Migration 53 — Remove GRNID from AssetMaster
--
-- AssetMaster.GRNID was added in Migration 29 (AssetGRNLink)
-- to link assets auto-created from GRN receive.
-- GRN → Asset auto-creation flow has been removed.
-- This migration drops the FK, index and column.
--
-- IDEMPOTENT — safe to re-run.
-- ============================================================

SET NOCOUNT ON;
PRINT '============================================================';
PRINT 'Migration 53 — Remove AssetMaster.GRNID — Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- 1. Drop FK constraint: FK_AssetMaster_GRN
-- ─────────────────────────────────────────────────────────────
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AssetMaster_GRN'
)
BEGIN
    ALTER TABLE AssetMaster DROP CONSTRAINT FK_AssetMaster_GRN;
    PRINT 'AssetMaster: FK_AssetMaster_GRN dropped.';
END
ELSE
    PRINT 'AssetMaster: FK_AssetMaster_GRN not found. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 2. Drop index: IX_AssetMaster_GRNID
-- ─────────────────────────────────────────────────────────────
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AssetMaster_GRNID'
      AND object_id = OBJECT_ID('AssetMaster')
)
BEGIN
    DROP INDEX IX_AssetMaster_GRNID ON AssetMaster;
    PRINT 'AssetMaster: Index IX_AssetMaster_GRNID dropped.';
END
ELSE
    PRINT 'AssetMaster: Index IX_AssetMaster_GRNID not found. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 3. Drop GRNID column
-- ─────────────────────────────────────────────────────────────
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AssetMaster' AND COLUMN_NAME = 'GRNID'
)
BEGIN
    ALTER TABLE AssetMaster DROP COLUMN GRNID;
    PRINT 'AssetMaster: GRNID column dropped.';
END
ELSE
    PRINT 'AssetMaster: GRNID already removed. Skipping.';
GO

PRINT '============================================================';
PRINT 'Migration 53 — Remove AssetMaster.GRNID — Completed.';
PRINT '============================================================';
