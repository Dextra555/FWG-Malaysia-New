-- ============================================================
-- INVENTORY TEST DATA CLEANUP SCRIPT
-- Deletes ALL data from all Inventory tables
-- (Foreign key order: child tables first, parent tables last)
--
-- ⚠️  WARNING: USE FOR TESTING ONLY — NOT FOR PRODUCTION
-- ⚠️  This deletes ALL rows. Tables are NOT dropped.
-- ⚠️  Master tables (BrandMaster, UOMMaster etc.) seed data
--     will also be removed. Re-run migration to restore seeds.
--
-- Safe to re-run — empty tables will just skip quietly.
-- ============================================================

PRINT '============================================================';
PRINT ' INVENTORY TEST DATA CLEANUP - Starting...';
PRINT '============================================================';

-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : Asset Location Tracking
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('AssetLocationLog') IS NOT NULL
BEGIN
    DELETE FROM AssetLocationLog;
    PRINT 'AssetLocationLog: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'AssetLocationLog: Table does not exist. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : Asset Management (Part 6)
-- Child → Parent order
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('AssetMovementHistory') IS NOT NULL
BEGIN
    DELETE FROM AssetMovementHistory;
    PRINT 'AssetMovementHistory: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'AssetMovementHistory: Table does not exist. Skipping.';

IF OBJECT_ID('AssetDisposal') IS NOT NULL
BEGIN
    DELETE FROM AssetDisposal;
    PRINT 'AssetDisposal: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'AssetDisposal: Table does not exist. Skipping.';

IF OBJECT_ID('AssetDamage') IS NOT NULL
BEGIN
    DELETE FROM AssetDamage;
    PRINT 'AssetDamage: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'AssetDamage: Table does not exist. Skipping.';

IF OBJECT_ID('AssetMaintenance') IS NOT NULL
BEGIN
    DELETE FROM AssetMaintenance;
    PRINT 'AssetMaintenance: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'AssetMaintenance: Table does not exist. Skipping.';

IF OBJECT_ID('AssetReturn') IS NOT NULL
BEGIN
    DELETE FROM AssetReturn;
    PRINT 'AssetReturn: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'AssetReturn: Table does not exist. Skipping.';

IF OBJECT_ID('AssetTransfer') IS NOT NULL
BEGIN
    DELETE FROM AssetTransfer;
    PRINT 'AssetTransfer: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'AssetTransfer: Table does not exist. Skipping.';

IF OBJECT_ID('AssetAllocation') IS NOT NULL
BEGIN
    DELETE FROM AssetAllocation;
    PRINT 'AssetAllocation: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'AssetAllocation: Table does not exist. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : Stock Operations (Part 4)
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('StockDamageLoss') IS NOT NULL
BEGIN
    DELETE FROM StockDamageLoss;
    PRINT 'StockDamageLoss: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'StockDamageLoss: Table does not exist. Skipping.';

IF OBJECT_ID('StockAdjustment') IS NOT NULL
BEGIN
    DELETE FROM StockAdjustment;
    PRINT 'StockAdjustment: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'StockAdjustment: Table does not exist. Skipping.';

IF OBJECT_ID('StockTransferDetail') IS NOT NULL
BEGIN
    DELETE FROM StockTransferDetail;
    PRINT 'StockTransferDetail: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'StockTransferDetail: Table does not exist. Skipping.';

IF OBJECT_ID('StockTransfer') IS NOT NULL
BEGIN
    DELETE FROM StockTransfer;
    PRINT 'StockTransfer: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'StockTransfer: Table does not exist. Skipping.';

IF OBJECT_ID('StockIssueReturnDetail') IS NOT NULL
BEGIN
    DELETE FROM StockIssueReturnDetail;
    PRINT 'StockIssueReturnDetail: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'StockIssueReturnDetail: Table does not exist. Skipping.';

IF OBJECT_ID('StockIssueReturn') IS NOT NULL
BEGIN
    DELETE FROM StockIssueReturn;
    PRINT 'StockIssueReturn: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'StockIssueReturn: Table does not exist. Skipping.';

IF OBJECT_ID('StockIssueDocDetail') IS NOT NULL
BEGIN
    DELETE FROM StockIssueDocDetail;
    PRINT 'StockIssueDocDetail: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'StockIssueDocDetail: Table does not exist. Skipping.';

IF OBJECT_ID('StockIssueDoc') IS NOT NULL
BEGIN
    DELETE FROM StockIssueDoc;
    PRINT 'StockIssueDoc: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'StockIssueDoc: Table does not exist. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 4 : Purchase Flow (Part 3)
-- PR → PO → GRN → PurchaseReturn order (reverse for delete)
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('PurchaseReturnDetail') IS NOT NULL
BEGIN
    DELETE FROM PurchaseReturnDetail;
    PRINT 'PurchaseReturnDetail: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'PurchaseReturnDetail: Table does not exist. Skipping.';

IF OBJECT_ID('PurchaseReturn') IS NOT NULL
BEGIN
    DELETE FROM PurchaseReturn;
    PRINT 'PurchaseReturn: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'PurchaseReturn: Table does not exist. Skipping.';

IF OBJECT_ID('GoodsReceiptNoteDetail') IS NOT NULL
BEGIN
    DELETE FROM GoodsReceiptNoteDetail;
    PRINT 'GoodsReceiptNoteDetail: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'GoodsReceiptNoteDetail: Table does not exist. Skipping.';

IF OBJECT_ID('GoodsReceiptNote') IS NOT NULL
BEGIN
    DELETE FROM GoodsReceiptNote;
    PRINT 'GoodsReceiptNote: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'GoodsReceiptNote: Table does not exist. Skipping.';

IF OBJECT_ID('PurchaseOrderDetail') IS NOT NULL
BEGIN
    DELETE FROM PurchaseOrderDetail;
    PRINT 'PurchaseOrderDetail: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'PurchaseOrderDetail: Table does not exist. Skipping.';

IF OBJECT_ID('PurchaseOrder') IS NOT NULL
BEGIN
    DELETE FROM PurchaseOrder;
    PRINT 'PurchaseOrder: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'PurchaseOrder: Table does not exist. Skipping.';

IF OBJECT_ID('PurchaseRequisitionDetail') IS NOT NULL
BEGIN
    DELETE FROM PurchaseRequisitionDetail;
    PRINT 'PurchaseRequisitionDetail: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'PurchaseRequisitionDetail: Table does not exist. Skipping.';

IF OBJECT_ID('PurchaseRequisition') IS NOT NULL
BEGIN
    DELETE FROM PurchaseRequisition;
    PRINT 'PurchaseRequisition: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'PurchaseRequisition: Table does not exist. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 5 : Stock Ledger + Opening Stock (Part 2)
-- StockLedger first (references OpeningStock by ReferenceID)
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('StockLedger') IS NOT NULL
BEGIN
    DELETE FROM StockLedger;
    PRINT 'StockLedger: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'StockLedger: Table does not exist. Skipping.';

IF OBJECT_ID('OpeningStock') IS NOT NULL
BEGIN
    DELETE FROM OpeningStock;
    PRINT 'OpeningStock: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'OpeningStock: Table does not exist. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 6 : AssetMaster (Part 6 parent)
-- After all child tables (Allocation, Transfer etc.) cleared
-- ─────────────────────────────────────────────────────────────

-- Reset location columns on AssetMaster (don't delete the assets,
-- just wipe the location tracking data)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='CurrentLocation')
BEGIN
    UPDATE AssetMaster
    SET CurrentLocation    = NULL,
        LocationStatus     = NULL,
        LocationUpdatedBy  = NULL,
        LocationUpdatedDate = NULL;
    PRINT 'AssetMaster: Location columns reset to NULL.';
END

-- Clear ALL asset records (after child tables cleared above)
IF OBJECT_ID('AssetMaster') IS NOT NULL
BEGIN
    DELETE FROM AssetMaster;
    PRINT 'AssetMaster: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'AssetMaster: Table does not exist. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 7 : Item Master (Part 1 parent)
-- Must come after all tables that FK to ItemMaster
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('ItemMaster') IS NOT NULL
BEGIN
    DELETE FROM ItemMaster;
    PRINT 'ItemMaster: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'ItemMaster: Table does not exist. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 8 : Masters (Part 1)
-- Last — these are referenced by all other tables
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('InventoryCategory') IS NOT NULL
BEGIN
    -- Self-referential FK (ParentCategoryID) — clear parent ref first
    UPDATE InventoryCategory SET ParentCategoryID = NULL;
    DELETE FROM InventoryCategory;
    PRINT 'InventoryCategory: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'InventoryCategory: Table does not exist. Skipping.';

IF OBJECT_ID('WarehouseMaster') IS NOT NULL
BEGIN
    DELETE FROM WarehouseMaster;
    PRINT 'WarehouseMaster: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'WarehouseMaster: Table does not exist. Skipping.';

IF OBJECT_ID('BrandMaster') IS NOT NULL
BEGIN
    DELETE FROM BrandMaster;
    PRINT 'BrandMaster: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'BrandMaster: Table does not exist. Skipping.';

IF OBJECT_ID('UOMMaster') IS NOT NULL
BEGIN
    DELETE FROM UOMMaster;
    PRINT 'UOMMaster: Cleared (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted).';
END
ELSE PRINT 'UOMMaster: Table does not exist. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 9 : IDENTITY RESET
-- Resets auto-increment counters so IDs start from 1 again
-- ─────────────────────────────────────────────────────────────

PRINT '';
PRINT '-- Resetting IDENTITY counters --';

DECLARE @tables TABLE (tname NVARCHAR(100));
INSERT INTO @tables VALUES
    ('AssetLocationLog'),
    ('AssetMovementHistory'),
    ('AssetDisposal'),
    ('AssetDamage'),
    ('AssetMaintenance'),
    ('AssetReturn'),
    ('AssetTransfer'),
    ('AssetAllocation'),
    ('AssetMaster'),
    ('StockDamageLoss'),
    ('StockAdjustment'),
    ('StockTransferDetail'),
    ('StockTransfer'),
    ('StockIssueReturnDetail'),
    ('StockIssueReturn'),
    ('StockIssueDocDetail'),
    ('StockIssueDoc'),
    ('PurchaseReturnDetail'),
    ('PurchaseReturn'),
    ('GoodsReceiptNoteDetail'),
    ('GoodsReceiptNote'),
    ('PurchaseOrderDetail'),
    ('PurchaseOrder'),
    ('PurchaseRequisitionDetail'),
    ('PurchaseRequisition'),
    ('StockLedger'),
    ('OpeningStock'),
    ('ItemMaster'),
    ('InventoryCategory'),
    ('WarehouseMaster'),
    ('BrandMaster'),
    ('UOMMaster');

DECLARE @tname NVARCHAR(100);
DECLARE @sql   NVARCHAR(300);

DECLARE cur CURSOR FOR SELECT tname FROM @tables;
OPEN cur;
FETCH NEXT FROM cur INTO @tname;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(@tname) IS NOT NULL
    BEGIN
        SET @sql = 'DBCC CHECKIDENT (''' + @tname + ''', RESEED, 0)';
        EXEC sp_executesql @sql;
        PRINT 'IDENTITY reset: ' + @tname;
    END
    FETCH NEXT FROM cur INTO @tname;
END

CLOSE cur;
DEALLOCATE cur;


-- ─────────────────────────────────────────────────────────────
PRINT '';
PRINT '============================================================';
PRINT ' INVENTORY TEST DATA CLEANUP - Completed Successfully.';
PRINT ' All inventory tables cleared. IDs reset to start from 1.';
PRINT ' Re-run migration scripts to restore seed data.';
PRINT '============================================================';
