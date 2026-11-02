-- ============================================================
-- OBMS Inventory - ItemMaster Size Column Migration
--
-- Purpose : Add one nullable `Size` column to the existing
--           ItemMaster table so that each item (e.g. a uniform
--           size like S / M / L / XL) can be maintained as a
--           separate Item Master record with its own Item ID
--           and Item Code.
--
-- This script is IDEMPOTENT:
--   - Already existing column is skipped
--   - Safe to re-run without errors
--
-- Rules followed (per project convention):
--   * Size is optional (NULL) — existing items without a size
--     continue to work without any change.
--   * No Parent Item / Variant system, no separate Size Master,
--     no duplicate Category Master, and no new tables.
--   * Existing stock balances, transactions and calculations
--     are NOT touched — Size is a display/identity attribute.
--
-- Target database : OBMSUATNEW (or live equivalent)
-- Last updated    : 2026
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS ItemMaster Size Column Migration - Starting...';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : ADD Size COLUMN TO ItemMaster
-- ─────────────────────────────────────────────────────────────
--
-- Convention matches existing nullable text columns such as
-- `Code NVARCHAR(50) NULL`. Size is free-text (e.g. 'S', 'M',
-- 'L', 'XL', '32', '34') and may be left blank.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ItemMaster' AND COLUMN_NAME = 'Size')
BEGIN
    ALTER TABLE ItemMaster ADD Size NVARCHAR(50) NULL;
    PRINT 'ItemMaster.Size column added.';
END
ELSE
    PRINT 'ItemMaster.Size already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : REFRESH VWStockLedger VIEW
-- ─────────────────────────────────────────────────────────────
--
-- The Stock Ledger Report view already joins ItemMaster; we only
-- surface the new `Size` column so size-wise stock can be shown
-- where needed. No transaction calculations are changed — only
-- one display column is added to the SELECT list.
--
-- The view is dropped and recreated (same pattern as migration 17)
-- so the new column is always present.
-- ─────────────────────────────────────────────────────────────

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
    im.Size                     AS Size,

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
    -- (UNCHANGED — adding the Size column does not affect this.)
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

PRINT 'VWStockLedger: View refreshed with Size column.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : REFRESH sp_InventoryStockLedger
-- ─────────────────────────────────────────────────────────────
--
-- The stored procedure now returns the new `Size` column so the
-- Stock Ledger Report (StockLedgerReport.aspx) can display
-- size-wise stock. The WHERE / ORDER BY logic is unchanged.
-- ─────────────────────────────────────────────────────────────

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
        v.Size,
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

PRINT 'sp_InventoryStockLedger: Refreshed with Size column.';


PRINT '';
PRINT '============================================================';
PRINT 'OBMS ItemMaster Size Column Migration - Completed Successfully.';
PRINT '============================================================';

-- ─────────────────────────────────────────────────────────────
-- VERIFICATION QUERIES (run manually after executing):
--
-- 1. Confirm the column exists:
--    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
--    FROM INFORMATION_SCHEMA.COLUMNS
--    WHERE TABLE_NAME = 'ItemMaster' AND COLUMN_NAME = 'Size';
--
-- 2. Confirm existing items still load (Size NULL is allowed):
--    SELECT TOP 5 ID, Code, Name, Size FROM ItemMaster ORDER BY ID;
--
-- 3. Confirm the view returns the new column:
--    SELECT TOP 5 ItemCode, ItemName, Size, WarehouseName,
--                  InQty, OutQty, RunningBalance
--    FROM VWStockLedger ORDER BY TransactionDate DESC, ID DESC;
-- ─────────────────────────────────────────────────────────────
