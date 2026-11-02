-- ============================================================
-- OBMS Inventory Part 9 - Stock Ledger Crystal Report
-- Migration Script
--
-- This script is IDEMPOTENT:
--   - View is dropped and recreated (safe to re-run)
--   - ScreenList entries are skipped if already present
--
-- Target database : OBMSUATNEW (or live equivalent)
-- Last updated    : 2026
--
-- What this script does:
--   1. Creates VWStockLedger view
--      - Joins StockLedger with WarehouseMaster, ItemMaster,
--        UOMMaster, InventoryCategory, BrandMaster
--      - Computes RunningBalance using SQL window function
--        (SUM OVER PARTITION BY WarehouseID, ItemID ORDER BY TransactionDate, ID)
--      - This view is the data source for StockLedger.rpt
--      - RunningBalance from this view must reconcile exactly
--        with the API screen (InventoryRepository.GetStockLedger)
--
--   2. Registers ScreenList entries for:
--      - 'Stock Ledger Report'        (new - Part 9)
--      - 'HQ Stock Ledger Report'     (fix missing entry)
--      - 'Branch Stock Ledger Report' (fix missing entry)
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Inventory Part 9 - Stock Ledger Report Migration';
PRINT 'Starting...';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : VWStockLedger VIEW
-- ─────────────────────────────────────────────────────────────
--
-- Business rules reflected in this view:
--   * RunningBalance = cumulative SUM(InQty - OutQty)
--     partitioned per (WarehouseID, ItemID), ordered by
--     TransactionDate ASC, ID ASC — matches the C# accumulator
--     in InventoryRepository.GetStockLedger exactly.
--   * UOMMaster, BrandMaster are LEFT JOINed (nullable on ItemMaster)
--   * InventoryCategory is LEFT JOINed (CategoryID is always set
--     but view is defensive with LEFT JOIN to avoid data issues)
--   * TransactionTypeLabel maps the code to a user-friendly string
--     using CASE — same mapping as the C# typeLabels dictionary
-- ─────────────────────────────────────────────────────────────

PRINT '';
PRINT 'Section 1: Creating VWStockLedger view...';

-- Drop and recreate so the view always reflects the latest definition
IF OBJECT_ID('VWStockLedger', 'V') IS NOT NULL
BEGIN
    DROP VIEW VWStockLedger;
    PRINT 'VWStockLedger: Existing view dropped.';
END
GO

CREATE VIEW VWStockLedger
AS
SELECT
    -- ── StockLedger core fields ──────────────────────────────
    sl.ID,
    sl.TransactionNo,
    sl.TransactionDate,
    sl.TransactionType,

    -- User-friendly label — matches C# typeLabels dictionary
    -- in InventoryRepository.GetStockLedger
    CASE sl.TransactionType
        WHEN 'OPENING'      THEN 'Opening Stock'
        WHEN 'STOCK_IN'     THEN 'Stock In'
        WHEN 'ISSUE'        THEN 'Stock Issue'
        WHEN 'ISSUE_RETURN' THEN 'Issue Return'
        WHEN 'TRANSFER_IN'  THEN 'Transfer In'
        WHEN 'TRANSFER_OUT' THEN 'Transfer Out'
        WHEN 'ADJ_PLUS'     THEN 'Adjustment (In)'
        WHEN 'ADJ_MINUS'    THEN 'Adjustment (Out)'
        WHEN 'PURCHASE_RTN' THEN 'Purchase Return'
        WHEN 'DAMAGE_LOSS'  THEN 'Damage / Loss'
        ELSE sl.TransactionType
    END                         AS TransactionTypeLabel,

    -- ── Warehouse ────────────────────────────────────────────
    sl.WarehouseID,
    wh.Name                     AS WarehouseName,

    -- ── Item ─────────────────────────────────────────────────
    sl.ItemID,
    im.Code                     AS ItemCode,
    im.Name                     AS ItemName,

    -- ── Category (LEFT JOIN — defensive) ────────────────────
    ic.Name                     AS CategoryName,

    -- ── Brand (LEFT JOIN — BrandID nullable on ItemMaster) ───
    bm.Name                     AS BrandName,

    -- ── UOM (LEFT JOIN — UOMID nullable on ItemMaster) ───────
    um.Name                     AS UOMName,

    -- ── Quantities ───────────────────────────────────────────
    sl.InQty,
    sl.OutQty,

    -- ── Running Balance ──────────────────────────────────────
    -- Cumulative SUM(InQty - OutQty) per warehouse+item,
    -- ordered by TransactionDate ASC, ID ASC.
    -- This matches the C# accumulator:
    --   runningBalances[key] += row.InQty - row.OutQty
    -- Rows with the same date but different IDs are ordered
    -- by ID to ensure deterministic balance calculation.
    SUM(sl.InQty - sl.OutQty) OVER (
        PARTITION BY sl.WarehouseID, sl.ItemID
        ORDER BY sl.TransactionDate ASC, sl.ID ASC
        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
    )                           AS RunningBalance,

    -- ── Cost & References ────────────────────────────────────
    sl.UnitCost,
    sl.ReferenceType,
    sl.ReferenceID,
    sl.ReferenceNo,
    sl.Remarks,

    -- ── Audit ────────────────────────────────────────────────
    sl.LASTUPDATE,
    sl.LastUpdatedBy

FROM StockLedger sl

    -- Warehouse — INNER JOIN (WarehouseID is NOT NULL and FK enforced)
    INNER JOIN WarehouseMaster wh
        ON sl.WarehouseID = wh.ID

    -- Item — INNER JOIN (ItemID is NOT NULL and FK enforced)
    INNER JOIN ItemMaster im
        ON sl.ItemID = im.ID

    -- Category — LEFT JOIN (defensive; CategoryID should always be set)
    LEFT JOIN InventoryCategory ic
        ON im.CategoryID = ic.ID

    -- Brand — LEFT JOIN (BrandID is nullable on ItemMaster)
    LEFT JOIN BrandMaster bm
        ON im.BrandID = bm.ID

    -- UOM — LEFT JOIN (UOMID is nullable on ItemMaster)
    LEFT JOIN UOMMaster um
        ON im.UOMID = um.ID;

GO

PRINT 'VWStockLedger: View created successfully.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : SCREENLIST ENTRIES
-- ─────────────────────────────────────────────────────────────
--
-- Screen names must match EXACTLY what the Angular components
-- pass to getUserAccessRights(userName, screenName).
--
-- New entry:
--   'Stock Ledger Report'        → new Part 9 Crystal Report
--
-- Fix missing entries (Angular components already use these
-- screen names but they were never inserted into ScreenList):
--   'HQ Stock Ledger Report'     → HQStockLedgerComponent
--   'Branch Stock Ledger Report' → BranchStockLedgerComponent
-- ─────────────────────────────────────────────────────────────

PRINT '';
PRINT 'Section 2: Registering ScreenList entries...';

-- Stock Ledger Report (new — Part 9)
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Ledger Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Ledger Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: ''Stock Ledger Report'' added.';
END
ELSE
    PRINT 'ScreenList: ''Stock Ledger Report'' already exists. Skipping.';

-- HQ Stock Ledger Report (fix missing entry)
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'HQ Stock Ledger Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('HQ Stock Ledger Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: ''HQ Stock Ledger Report'' added.';
END
ELSE
    PRINT 'ScreenList: ''HQ Stock Ledger Report'' already exists. Skipping.';

-- Branch Stock Ledger Report (fix missing entry)
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Branch Stock Ledger Report')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Branch Stock Ledger Report', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: ''Branch Stock Ledger Report'' added.';
END
ELSE
    PRINT 'ScreenList: ''Branch Stock Ledger Report'' already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- VERIFICATION QUERIES
-- ─────────────────────────────────────────────────────────────
-- Run these manually after executing this migration to verify:
--
-- 1. Confirm view exists and columns are correct:
--    SELECT TOP 5 * FROM VWStockLedger ORDER BY TransactionDate DESC, ID DESC;
--
-- 2. Confirm running balance calculation (pick one item+warehouse):
--    SELECT TransactionDate, TransactionNo, TransactionType,
--           InQty, OutQty, RunningBalance
--    FROM VWStockLedger
--    WHERE WarehouseID = 1 AND ItemID = 1
--    ORDER BY TransactionDate, ID;
--
-- 3. Confirm ScreenList entries:
--    SELECT ScreenName, Category FROM ScreenList
--    WHERE ScreenName IN (
--        'Stock Ledger Report',
--        'HQ Stock Ledger Report',
--        'Branch Stock Ledger Report'
--    );
--
-- 4. Reconcile with API screen — run this and compare with the
--    Angular Stock Ledger screen for the same filters:
--    SELECT * FROM VWStockLedger
--    WHERE WarehouseID = <id>
--      AND TransactionDate BETWEEN '<start>' AND '<end>'
--    ORDER BY ItemID, TransactionDate, ID;
-- ─────────────────────────────────────────────────────────────

PRINT '';
PRINT '============================================================';
PRINT 'OBMS Inventory Part 9 Migration - Completed Successfully.';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : STORED PROCEDURE sp_InventoryStockLedger
-- ─────────────────────────────────────────────────────────────
--
-- Purpose : Returns filtered Stock Ledger rows for the PDF report.
--           Called by StockLedgerReport.aspx.cs via ADO.NET.
--
-- Parameters:
--   @WarehouseID     INT           0 = all warehouses
--   @ItemID          INT           0 = all items
--   @TransactionType NVARCHAR(50)  'ALL' = all types
--   @FromDate        DATETIME      start of date range (inclusive)
--   @ToDate          DATETIME      end of date range (inclusive, extended to 23:59:59)
--
-- Data source: VWStockLedger view (Section 1 of this migration).
-- RunningBalance in the view is partitioned per (WarehouseID, ItemID)
-- across ALL history — not just the filtered window. This is intentional:
-- it shows the true running balance at each point in time, matching the
-- operational Stock Ledger screen exactly.
--
-- Sort order: ItemName ASC, TransactionDate ASC, ID ASC
-- This matches InventoryRepository.GetStockLedger order and ensures
-- RunningBalance values are sequential and correct.
-- ─────────────────────────────────────────────────────────────

PRINT '';
PRINT 'Section 3: Creating sp_InventoryStockLedger...';

IF OBJECT_ID('sp_InventoryStockLedger', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE sp_InventoryStockLedger;
    PRINT 'sp_InventoryStockLedger: Existing procedure dropped.';
END
GO

CREATE PROCEDURE sp_InventoryStockLedger
    @WarehouseID     INT            = 0,
    @ItemID          INT            = 0,
    @TransactionType NVARCHAR(50)   = 'ALL',
    @FromDate        DATETIME       = NULL,
    @ToDate          DATETIME       = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Default date range: current month if not supplied
    IF @FromDate IS NULL
        SET @FromDate = DATEADD(DAY, 1 - DAY(GETDATE()), CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL
        SET @ToDate = GETDATE();

    -- Extend ToDate to end of day so full day is included
    SET @ToDate = CAST(@ToDate AS DATE);
    SET @ToDate = DATEADD(SECOND, 86399, CAST(@ToDate AS DATETIME)); -- 23:59:59

    SELECT
        v.ID,
        v.TransactionNo,
        v.TransactionDate,
        v.TransactionType,
        v.TransactionTypeLabel,
        v.WarehouseID,
        v.WarehouseName,
        v.ItemID,
        v.ItemCode,
        v.ItemName,
        v.CategoryName,
        v.BrandName,
        v.UOMName,
        v.InQty,
        v.OutQty,
        v.RunningBalance,
        v.UnitCost,
        v.ReferenceType,
        v.ReferenceID,
        v.ReferenceNo,
        v.Remarks
    FROM VWStockLedger v
    WHERE
        -- Date range filter (always applied)
        v.TransactionDate >= @FromDate
        AND v.TransactionDate <= @ToDate

        -- Warehouse filter (0 = all)
        AND (@WarehouseID = 0 OR v.WarehouseID = @WarehouseID)

        -- Item filter (0 = all)
        AND (@ItemID = 0 OR v.ItemID = @ItemID)

        -- TransactionType filter ('ALL' = all types)
        AND (@TransactionType = 'ALL' OR v.TransactionType = @TransactionType)

    ORDER BY
        v.ItemName       ASC,
        v.TransactionDate ASC,
        v.ID             ASC;
END
GO

PRINT 'sp_InventoryStockLedger: Created successfully.';
PRINT '';
PRINT '============================================================';
PRINT 'Section 3 Complete.';
PRINT '============================================================';

-- ─────────────────────────────────────────────────────────────
-- TEST QUERY (run manually to verify SP):
--
-- EXEC sp_InventoryStockLedger
--     @WarehouseID     = 1,
--     @ItemID          = 0,
--     @TransactionType = 'ALL',
--     @FromDate        = '2026-01-01',
--     @ToDate          = '2026-12-31';
-- ─────────────────────────────────────────────────────────────
