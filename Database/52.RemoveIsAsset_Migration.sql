-- ============================================================
-- OBMS Migration 52 — Remove IsAsset & AssetID columns
--
-- IsAsset flag and related AssetID columns are removed from:
--   1. GoodsReceiptNoteDetail  → IsAsset, AssetID
--   2. PurchaseOrderDetail     → IsAsset, AssetID
--   3. ItemMaster              → IsAsset
--
-- DEFAULT constraints must be dropped before dropping the column.
-- Constraint names are looked up dynamically (sys.default_constraints).
--
-- IDEMPOTENT — safe to re-run.
-- ============================================================

SET NOCOUNT ON;
PRINT '============================================================';
PRINT 'Migration 52 — Remove IsAsset columns — Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- Helper: drop a named FK constraint if it exists
-- ─────────────────────────────────────────────────────────────
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GRNDetail_Asset'
)
BEGIN
    ALTER TABLE GoodsReceiptNoteDetail DROP CONSTRAINT FK_GRNDetail_Asset;
    PRINT 'GoodsReceiptNoteDetail: FK_GRNDetail_Asset dropped.';
END
ELSE
    PRINT 'GoodsReceiptNoteDetail: FK_GRNDetail_Asset not found. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 1-A. GoodsReceiptNoteDetail → AssetID
-- ─────────────────────────────────────────────────────────────
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'GoodsReceiptNoteDetail' AND COLUMN_NAME = 'AssetID'
)
BEGIN
    ALTER TABLE GoodsReceiptNoteDetail DROP COLUMN AssetID;
    PRINT 'GoodsReceiptNoteDetail: AssetID column dropped.';
END
ELSE
    PRINT 'GoodsReceiptNoteDetail: AssetID already removed. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 1-B. GoodsReceiptNoteDetail → IsAsset  (drop DEFAULT first)
-- ─────────────────────────────────────────────────────────────
DECLARE @df NVARCHAR(256);
SELECT @df = dc.name
FROM   sys.default_constraints dc
JOIN   sys.columns              c  ON dc.parent_object_id = c.object_id
                                   AND dc.parent_column_id = c.column_id
JOIN   sys.tables               t  ON c.object_id = t.object_id
WHERE  t.name = 'GoodsReceiptNoteDetail'
AND    c.name = 'IsAsset';

IF @df IS NOT NULL
BEGIN
    EXEC('ALTER TABLE GoodsReceiptNoteDetail DROP CONSTRAINT [' + @df + ']');
    PRINT 'GoodsReceiptNoteDetail: DEFAULT constraint on IsAsset dropped.';
END
GO

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'GoodsReceiptNoteDetail' AND COLUMN_NAME = 'IsAsset'
)
BEGIN
    ALTER TABLE GoodsReceiptNoteDetail DROP COLUMN IsAsset;
    PRINT 'GoodsReceiptNoteDetail: IsAsset column dropped.';
END
ELSE
    PRINT 'GoodsReceiptNoteDetail: IsAsset already removed. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 2-A. PurchaseOrderDetail → AssetID
-- ─────────────────────────────────────────────────────────────
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PurchaseOrderDetail' AND COLUMN_NAME = 'AssetID'
)
BEGIN
    ALTER TABLE PurchaseOrderDetail DROP COLUMN AssetID;
    PRINT 'PurchaseOrderDetail: AssetID column dropped.';
END
ELSE
    PRINT 'PurchaseOrderDetail: AssetID already removed. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 2-B. PurchaseOrderDetail → IsAsset  (drop DEFAULT first)
-- ─────────────────────────────────────────────────────────────
DECLARE @df2 NVARCHAR(256);
SELECT @df2 = dc.name
FROM   sys.default_constraints dc
JOIN   sys.columns              c  ON dc.parent_object_id = c.object_id
                                   AND dc.parent_column_id = c.column_id
JOIN   sys.tables               t  ON c.object_id = t.object_id
WHERE  t.name = 'PurchaseOrderDetail'
AND    c.name = 'IsAsset';

IF @df2 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE PurchaseOrderDetail DROP CONSTRAINT [' + @df2 + ']');
    PRINT 'PurchaseOrderDetail: DEFAULT constraint on IsAsset dropped.';
END
GO

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PurchaseOrderDetail' AND COLUMN_NAME = 'IsAsset'
)
BEGIN
    ALTER TABLE PurchaseOrderDetail DROP COLUMN IsAsset;
    PRINT 'PurchaseOrderDetail: IsAsset column dropped.';
END
ELSE
    PRINT 'PurchaseOrderDetail: IsAsset already removed. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 3. ItemMaster → IsAsset  (drop DEFAULT first)
-- ─────────────────────────────────────────────────────────────
DECLARE @df3 NVARCHAR(256);
SELECT @df3 = dc.name
FROM   sys.default_constraints dc
JOIN   sys.columns              c  ON dc.parent_object_id = c.object_id
                                   AND dc.parent_column_id = c.column_id
JOIN   sys.tables               t  ON c.object_id = t.object_id
WHERE  t.name = 'ItemMaster'
AND    c.name = 'IsAsset';

IF @df3 IS NOT NULL
BEGIN
    EXEC('ALTER TABLE ItemMaster DROP CONSTRAINT [' + @df3 + ']');
    PRINT 'ItemMaster: DEFAULT constraint on IsAsset dropped.';
END
GO

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ItemMaster' AND COLUMN_NAME = 'IsAsset'
)
BEGIN
    ALTER TABLE ItemMaster DROP COLUMN IsAsset;
    PRINT 'ItemMaster: IsAsset column dropped.';
END
ELSE
    PRINT 'ItemMaster: IsAsset already removed. Skipping.';
GO

PRINT '============================================================';
PRINT 'Migration 52 — Remove IsAsset columns — Completed.';
PRINT '============================================================';
