-- ============================================================
-- OBMS Migration 53 — PO Service Line (Free-text items)
--
-- Allows PO lines that are NOT linked to an ItemMaster record.
-- This supports "Service Bill" / one-time expense lines such as
-- consultancy, delivery charges, or any ad-hoc purchase.
--
-- Changes to PurchaseOrderDetail:
--   1. ItemID         → NULL allowed  (was NOT NULL FK)
--   2. ServiceDescription NVARCHAR(250) NULL added
--   3. IsServiceLine  BIT NOT NULL DEFAULT 0 added
--
-- NOTE: The FK constraint FK_PODetail_Item (if it exists) must be
--       dropped before making ItemID nullable.
--
-- IDEMPOTENT — safe to re-run.
-- ============================================================

SET NOCOUNT ON;
PRINT '============================================================';
PRINT 'Migration 53 — PO Service Line — Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- 1. Drop FK constraint on PurchaseOrderDetail.ItemID
--    (constraint name may vary — look it up dynamically)
-- ─────────────────────────────────────────────────────────────
DECLARE @fkName NVARCHAR(256);

SELECT @fkName = fk.name
FROM   sys.foreign_keys        fk
JOIN   sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN   sys.columns             c   ON fkc.parent_object_id = c.object_id
                                   AND fkc.parent_column_id  = c.column_id
JOIN   sys.tables              t   ON c.object_id = t.object_id
WHERE  t.name = 'PurchaseOrderDetail'
AND    c.name = 'ItemID';

IF @fkName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE PurchaseOrderDetail DROP CONSTRAINT [' + @fkName + ']');
    PRINT 'PurchaseOrderDetail: FK on ItemID dropped (' + @fkName + ').';
END
ELSE
    PRINT 'PurchaseOrderDetail: No FK found on ItemID. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 2. Make ItemID nullable
-- ─────────────────────────────────────────────────────────────
IF EXISTS (
    SELECT 1
    FROM   INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME   = 'PurchaseOrderDetail'
    AND    COLUMN_NAME  = 'ItemID'
    AND    IS_NULLABLE  = 'NO'
)
BEGIN
    ALTER TABLE PurchaseOrderDetail ALTER COLUMN ItemID INT NULL;
    PRINT 'PurchaseOrderDetail: ItemID is now nullable.';
END
ELSE
    PRINT 'PurchaseOrderDetail: ItemID already nullable. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 3. Re-add FK as nullable (ON DELETE SET NULL)
--    Only ItemMaster records that exist are enforced;
--    NULL means "service / free-text line".
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PODetail_Item'
)
BEGIN
    ALTER TABLE PurchaseOrderDetail
        ADD CONSTRAINT FK_PODetail_Item
        FOREIGN KEY (ItemID) REFERENCES ItemMaster(ID)
        ON DELETE SET NULL;
    PRINT 'PurchaseOrderDetail: FK_PODetail_Item re-added (nullable).';
END
ELSE
    PRINT 'PurchaseOrderDetail: FK_PODetail_Item already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 4. Add ServiceDescription column
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1
    FROM   INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME  = 'PurchaseOrderDetail'
    AND    COLUMN_NAME = 'ServiceDescription'
)
BEGIN
    ALTER TABLE PurchaseOrderDetail
        ADD ServiceDescription NVARCHAR(250) NULL;
    PRINT 'PurchaseOrderDetail: ServiceDescription column added.';
END
ELSE
    PRINT 'PurchaseOrderDetail: ServiceDescription already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- 5. Add IsServiceLine flag (BIT, default 0)
-- ─────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1
    FROM   INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME  = 'PurchaseOrderDetail'
    AND    COLUMN_NAME = 'IsServiceLine'
)
BEGIN
    ALTER TABLE PurchaseOrderDetail
        ADD IsServiceLine BIT NOT NULL CONSTRAINT DF_PODetail_IsServiceLine DEFAULT 0;
    PRINT 'PurchaseOrderDetail: IsServiceLine column added.';
END
ELSE
    PRINT 'PurchaseOrderDetail: IsServiceLine already exists. Skipping.';
GO

PRINT '============================================================';
PRINT 'Migration 53 — PO Service Line — Completed.';
PRINT '============================================================';

-- ─────────────────────────────────────────────────────────────
-- 6. GoodsReceiptNoteDetail → Make ItemID nullable
--    Service lines have no ItemMaster entry, so GRN detail rows
--    created for service lines will have ItemID = NULL.
-- ─────────────────────────────────────────────────────────────

-- 6-A. Drop FK on GoodsReceiptNoteDetail.ItemID (if it exists)
DECLARE @fkGrn NVARCHAR(256);

SELECT @fkGrn = fk.name
FROM   sys.foreign_keys        fk
JOIN   sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN   sys.columns             c   ON fkc.parent_object_id = c.object_id
                                   AND fkc.parent_column_id  = c.column_id
JOIN   sys.tables              t   ON c.object_id = t.object_id
WHERE  t.name = 'GoodsReceiptNoteDetail'
AND    c.name = 'ItemID';

IF @fkGrn IS NOT NULL
BEGIN
    EXEC('ALTER TABLE GoodsReceiptNoteDetail DROP CONSTRAINT [' + @fkGrn + ']');
    PRINT 'GoodsReceiptNoteDetail: FK on ItemID dropped (' + @fkGrn + ').';
END
ELSE
    PRINT 'GoodsReceiptNoteDetail: No FK found on ItemID. Skipping.';
GO

-- 6-B. Make ItemID nullable
IF EXISTS (
    SELECT 1
    FROM   INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME   = 'GoodsReceiptNoteDetail'
    AND    COLUMN_NAME  = 'ItemID'
    AND    IS_NULLABLE  = 'NO'
)
BEGIN
    ALTER TABLE GoodsReceiptNoteDetail ALTER COLUMN ItemID INT NULL;
    PRINT 'GoodsReceiptNoteDetail: ItemID is now nullable.';
END
ELSE
    PRINT 'GoodsReceiptNoteDetail: ItemID already nullable. Skipping.';
GO

-- 6-C. Re-add FK as nullable (ON DELETE SET NULL)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_GRNDetail_Item'
)
BEGIN
    ALTER TABLE GoodsReceiptNoteDetail
        ADD CONSTRAINT FK_GRNDetail_Item
        FOREIGN KEY (ItemID) REFERENCES ItemMaster(ID)
        ON DELETE SET NULL;
    PRINT 'GoodsReceiptNoteDetail: FK_GRNDetail_Item re-added (nullable).';
END
ELSE
    PRINT 'GoodsReceiptNoteDetail: FK_GRNDetail_Item already exists. Skipping.';
GO

PRINT '============================================================';
PRINT 'Migration 53 — PO + GRN Service Line — Completed.';
PRINT '============================================================';
