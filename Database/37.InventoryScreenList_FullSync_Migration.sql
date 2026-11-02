-- ============================================================
-- Migration 37: Inventory ScreenList Full Sync
-- ============================================================
-- Purpose : Ensures ALL Inventory pages appear in the
--           "Edit User Access Rights" screen under the
--           Inventory category.
--
-- This script is IDEMPOTENT — safe to re-run.
-- Uses IF NOT EXISTS on every INSERT.
--
-- Covers every Angular component that calls:
--   getUserAccessRights(user, '<ScreenName>')
-- with Category = 'Inventory'.
--
-- Run on  : OBMSUATNEW (or live equivalent)
-- Date    : 2026
-- ============================================================

PRINT '============================================================';
PRINT 'Migration 37: Inventory ScreenList Full Sync';
PRINT 'Starting...';
PRINT '============================================================';

-- ──────────────────────────────────────────────────────────────
-- SECTION 1 : Core Inventory Masters  (Migration 01)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Item Master')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Item Master', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Item Master';
END
ELSE PRINT 'Skip : Item Master';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Brand Master')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Brand Master', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Brand Master';
END
ELSE PRINT 'Skip : Brand Master';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'UOM Master')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('UOM Master', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: UOM Master';
END
ELSE PRINT 'Skip : UOM Master';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Warehouse Master')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Warehouse Master', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Warehouse Master';
END
ELSE PRINT 'Skip : Warehouse Master';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Inventory Category')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Inventory Category', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Inventory Category';
END
ELSE PRINT 'Skip : Inventory Category';

-- ──────────────────────────────────────────────────────────────
-- SECTION 2 : Stock Core  (Migration 02)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Opening Stock')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Opening Stock', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Opening Stock';
END
ELSE PRINT 'Skip : Opening Stock';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Ledger')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Ledger', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Ledger';
END
ELSE PRINT 'Skip : Stock Ledger';

-- ──────────────────────────────────────────────────────────────
-- SECTION 3 : Purchase Flow  (Migration 03)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Purchase Requisition')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Purchase Requisition', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Purchase Requisition';
END
ELSE PRINT 'Skip : Purchase Requisition';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Purchase Order')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Purchase Order', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Purchase Order';
END
ELSE PRINT 'Skip : Purchase Order';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Goods Receipt')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Goods Receipt', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Goods Receipt';
END
ELSE PRINT 'Skip : Goods Receipt';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Purchase Return')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Purchase Return', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Purchase Return';
END
ELSE PRINT 'Skip : Purchase Return';

-- ──────────────────────────────────────────────────────────────
-- SECTION 4 : Stock Operations  (Migration 04)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Issue')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Issue', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Issue';
END
ELSE PRINT 'Skip : Stock Issue';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Issue Return')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Issue Return', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Issue Return';
END
ELSE PRINT 'Skip : Stock Issue Return';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Transfer')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Transfer', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Transfer';
END
ELSE PRINT 'Skip : Stock Transfer';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Adjustment')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Adjustment', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Adjustment';
END
ELSE PRINT 'Skip : Stock Adjustment';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Damage / Loss')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Damage / Loss', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Damage / Loss';
END
ELSE PRINT 'Skip : Damage / Loss';

-- ──────────────────────────────────────────────────────────────
-- SECTION 5 : Employee / Summary screens  (Migration 05)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Employee Item Holding')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Employee Item Holding', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Employee Item Holding';
END
ELSE PRINT 'Skip : Employee Item Holding';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Balance Summary')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Balance Summary', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Balance Summary';
END
ELSE PRINT 'Skip : Stock Balance Summary';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Employee Issue Register')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Employee Issue Register', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Employee Issue Register';
END
ELSE PRINT 'Skip : Employee Issue Register';

-- ──────────────────────────────────────────────────────────────
-- SECTION 6 : Asset Management  (Migration 06)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Master')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Master', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Master';
END
ELSE PRINT 'Skip : Asset Master';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Allocation')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Allocation', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Allocation';
END
ELSE PRINT 'Skip : Asset Allocation';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Transfer')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Transfer', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Transfer';
END
ELSE PRINT 'Skip : Asset Transfer';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Return')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Return', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Return';
END
ELSE PRINT 'Skip : Asset Return';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Maintenance')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Maintenance', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Maintenance';
END
ELSE PRINT 'Skip : Asset Maintenance';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Damage')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Damage', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Damage';
END
ELSE PRINT 'Skip : Asset Damage';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Disposal')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Disposal', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Disposal';
END
ELSE PRINT 'Skip : Asset Disposal';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Movement History')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Movement History', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Movement History';
END
ELSE PRINT 'Skip : Asset Movement History';

-- ──────────────────────────────────────────────────────────────
-- SECTION 7 : Asset Location  (Migration 07)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Location')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Location', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Location';
END
ELSE PRINT 'Skip : Asset Location';

-- ──────────────────────────────────────────────────────────────
-- SECTION 8 : Branch Stock Request  (Migration 09)
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Branch Stock Request')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Branch Stock Request', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Branch Stock Request';
END
ELSE PRINT 'Skip : Branch Stock Request';

-- ──────────────────────────────────────────────────────────────
-- SECTION 9 : Approval / Notification screens
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Pending Approvals')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Pending Approvals', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Pending Approvals';
END
ELSE PRINT 'Skip : Pending Approvals';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Approval History')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Approval History', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Approval History';
END
ELSE PRINT 'Skip : Approval History';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Notifications')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Notifications', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Notifications';
END
ELSE PRINT 'Skip : Notifications';

-- ──────────────────────────────────────────────────────────────
-- SECTION 10 : Crystal Report / PDF screens (Migrations 17-36)
-- NEW pages — these appear in "Edit User Access Rights" now
-- ──────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Ledger Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Ledger Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Ledger Report';
END
ELSE PRINT 'Skip : Stock Ledger Report';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'HQ Stock Ledger Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('HQ Stock Ledger Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: HQ Stock Ledger Report';
END
ELSE PRINT 'Skip : HQ Stock Ledger Report';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Branch Stock Ledger Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Branch Stock Ledger Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Branch Stock Ledger Report';
END
ELSE PRINT 'Skip : Branch Stock Ledger Report';

-- ── NEW: Migration 30 ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Current Stock Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Current Stock Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Current Stock Report';
END
ELSE PRINT 'Skip : Current Stock Report';

-- ── NEW: Migration 31 ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Movement Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Movement Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Movement Report';
END
ELSE PRINT 'Skip : Stock Movement Report';

-- ── NEW: Migration 32 ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock In GRN Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock In GRN Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock In GRN Report';
END
ELSE PRINT 'Skip : Stock In GRN Report';

-- ── NEW: Migration 33 ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Issue List Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Issue List Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Issue List Report';
END
ELSE PRINT 'Skip : Stock Issue List Report';

-- ── NEW: Migration 34 ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Item Category Stock Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Item Category Stock Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Item Category Stock Report';
END
ELSE PRINT 'Skip : Item Category Stock Report';

-- ── NEW: Migration 35 ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Warehouse Stock Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Warehouse Stock Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Warehouse Stock Report';
END
ELSE PRINT 'Skip : Warehouse Stock Report';

-- ── NEW: Migration 36 ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Supplier Purchase Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Supplier Purchase Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Supplier Purchase Report';
END
ELSE PRINT 'Skip : Supplier Purchase Report';


-- ──────────────────────────────────────────────────────────────
-- VERIFICATION
-- ──────────────────────────────────────────────────────────────
-- Run this query after executing the migration to confirm
-- all entries are present:

-- SELECT ScreenName, Category
-- FROM ScreenList
-- WHERE Category = 'Inventory'
-- ORDER BY ScreenName;

-- Expected result: 35+ rows covering every Inventory screen.

-- ──────────────────────────────────────────────────────────────
-- SECTION 11 : NEW Report Pages (Migrations 38-44)
-- These were created AFTER Migration 37 and must be included
-- ──────────────────────────────────────────────────────────────

-- ── Migration 38: Low Stock Report ───────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Low Stock Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Low Stock Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Low Stock Report';
END
ELSE PRINT 'Skip : Low Stock Report';

-- ── Migration 39: Stock Transfer Report ──────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Transfer Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Transfer Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Transfer Report';
END
ELSE PRINT 'Skip : Stock Transfer Report';

-- ── Migration 40: Stock Adj Damage Loss Report ───────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Adj Damage Loss Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Adj Damage Loss Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Stock Adj Damage Loss Report';
END
ELSE PRINT 'Skip : Stock Adj Damage Loss Report';

-- ── Migration 41: Purchase Issue Return Report ───────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Purchase Issue Return Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Purchase Issue Return Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Purchase Issue Return Report';
END
ELSE PRINT 'Skip : Purchase Issue Return Report';

-- ── Migration 42: Inventory Valuation Report ─────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Inventory Valuation Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Inventory Valuation Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Inventory Valuation Report';
END
ELSE PRINT 'Skip : Inventory Valuation Report';

-- ── Migration 43: Branch Asset Report ────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Branch Asset Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Branch Asset Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Branch Asset Report';
END
ELSE PRINT 'Skip : Branch Asset Report';

-- ── Migration 44: Asset Movement Report ──────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Movement Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Movement Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'Added: Asset Movement Report';
END
ELSE PRINT 'Skip : Asset Movement Report';


-- ──────────────────────────────────────────────────────────────
-- VERIFICATION — run manually after migration
-- ──────────────────────────────────────────────────────────────
-- SELECT ScreenName, Category
-- FROM ScreenList
-- WHERE Category = 'Inventory'
-- ORDER BY ScreenName;
--
-- Expected: All 42+ Inventory screens listed.

PRINT '';
PRINT '============================================================';
PRINT 'Migration 37: Inventory ScreenList Full Sync - COMPLETE';
PRINT '============================================================';
