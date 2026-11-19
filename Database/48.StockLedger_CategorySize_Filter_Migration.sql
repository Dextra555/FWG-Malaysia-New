-- ============================================================
-- Migration 48: Stock Ledger Report — Category & Size Filters
--
-- Purpose:
--   1. Add sp_GetItemSizesByCategory — returns distinct non-null
--      Size values for items under a given category (and its
--      sub-categories). Used by the Angular Size dropdown.
--
--   2. Refresh sp_InventoryStockLedger — adds two new optional
--      parameters: @CategoryID and @Size for filtering.
--      Existing callers that pass no extra params are unchanged.
--
-- Design rules followed:
--   * No new tables, no separate Size Master.
--   * Category filtering includes parent AND all child categories.
--   * @CategoryID = 0 / NULL = All categories.
--   * @Size = '' / NULL      = All sizes (incl. NULL-size items).
--   * When @Size IS specified, items with NULL Size are excluded.
--   * Parameterised SQL only — no dynamic SQL string building.
--   * VWStockLedger already exposes CategoryName, im.CategoryID
--     and Size (added in migration 47) — no view change needed.
--
-- IDEMPOTENT: DROP + CREATE pattern, safe to re-run.
--
-- Target database : OBMSUATNEW (or live equivalent)
-- ============================================================

PRINT '============================================================';
PRINT 'Migration 48: Stock Ledger Category/Size Filters - Starting...';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : sp_GetItemSizesByCategory
--
-- Returns distinct Size values for the given CategoryID (0 = all).
-- Includes sub-categories automatically via the ParentCategoryID
-- self-referential FK on InventoryCategory.
-- Only non-NULL, non-empty sizes are returned.
-- Results are sorted naturally (numerically first, then alpha).
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_GetItemSizesByCategory', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE sp_GetItemSizesByCategory;
    PRINT 'sp_GetItemSizesByCategory: Existing procedure dropped.';
END
GO

CREATE PROCEDURE sp_GetItemSizesByCategory
    @CategoryID INT = 0      -- 0 = All categories
AS
BEGIN
    SET NOCOUNT ON;

    -- Collect the category IDs to filter against.
    -- If @CategoryID = 0, include all categories.
    -- Otherwise include the selected category AND its children
    -- (one level deep — ParentCategoryID relationship).

    IF @CategoryID = 0
    BEGIN
        -- All categories: return every distinct size across all items
        -- Wrap DISTINCT in a subquery so ORDER BY can use computed columns
        SELECT Size
        FROM (
            SELECT DISTINCT im.Size AS Size
            FROM ItemMaster im
            WHERE im.Size IS NOT NULL
              AND im.Size <> ''
        ) AS s
        ORDER BY
            -- Numeric sizes (28, 30, 32...) sort before text (S, M, L)
            CASE WHEN ISNUMERIC(s.Size) = 1 THEN 0 ELSE 1 END,
            CASE WHEN ISNUMERIC(s.Size) = 1 THEN CAST(s.Size AS FLOAT) ELSE NULL END,
            s.Size;
    END
    ELSE
    BEGIN
        -- Selected category + its sub-categories
        SELECT Size
        FROM (
            SELECT DISTINCT im.Size AS Size
            FROM ItemMaster im
            WHERE im.Size IS NOT NULL
              AND im.Size <> ''
              AND (
                  im.CategoryID = @CategoryID                          -- direct match
                  OR im.CategoryID IN (                                -- child categories
                      SELECT ID FROM InventoryCategory
                      WHERE ParentCategoryID = @CategoryID
                  )
              )
        ) AS s
        ORDER BY
            CASE WHEN ISNUMERIC(s.Size) = 1 THEN 0 ELSE 1 END,
            CASE WHEN ISNUMERIC(s.Size) = 1 THEN CAST(s.Size AS FLOAT) ELSE NULL END,
            s.Size;
    END
END
GO

PRINT 'sp_GetItemSizesByCategory: Created successfully.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : REFRESH sp_InventoryStockLedger
--             (used by StockLedgerReport.aspx PDF report)
--
-- Two new OPTIONAL parameters added:
--   @CategoryID INT      = 0   (0 = All)
--   @Size       NVARCHAR = ''  ('' = All; when set, NULL-size items excluded)
--
-- All existing parameters remain at the same positions with the
-- same defaults — existing callers work without change.
-- ─────────────────────────────────────────────────────────────

IF OBJECT_ID('sp_InventoryStockLedger', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE sp_InventoryStockLedger;
    PRINT 'sp_InventoryStockLedger: Existing procedure dropped.';
END
GO

CREATE PROCEDURE sp_InventoryStockLedger
    -- Existing parameters (unchanged)
    @WarehouseID     INT            = 0,
    @ItemID          INT            = 0,
    @TransactionType NVARCHAR(50)   = 'ALL',
    @FromDate        DATETIME       = NULL,
    @ToDate          DATETIME       = NULL,
    -- New optional parameters
    @CategoryID      INT            = 0,
    @Size            NVARCHAR(50)   = ''
AS
BEGIN
    SET NOCOUNT ON;

    -- Default date range: current month if not supplied
    IF @FromDate IS NULL
        SET @FromDate = DATEADD(DAY, 1 - DAY(GETDATE()), CAST(GETDATE() AS DATE));
    IF @ToDate IS NULL
        SET @ToDate = GETDATE();

    -- Extend ToDate to end of day
    SET @ToDate = CAST(@ToDate AS DATE);
    SET @ToDate = DATEADD(SECOND, 86399, CAST(@ToDate AS DATETIME)); -- 23:59:59

    -- Normalize empty string to NULL for cleaner comparisons
    IF @Size = '' SET @Size = NULL;

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

    -- Join ItemMaster to access CategoryID for filtering
    -- (VWStockLedger already has CategoryName but not CategoryID)
    INNER JOIN ItemMaster im
        ON v.ItemID = im.ID

    WHERE
        -- Date range
        v.TransactionDate >= @FromDate
        AND v.TransactionDate <= @ToDate

        -- Warehouse (0 = all)
        AND (@WarehouseID = 0 OR v.WarehouseID = @WarehouseID)

        -- Item (0 = all)
        AND (@ItemID = 0 OR v.ItemID = @ItemID)

        -- Transaction type ('ALL' = all)
        AND (@TransactionType = 'ALL' OR v.TransactionType = @TransactionType)

        -- Category filter: 0 = all; otherwise match selected category
        -- AND its sub-categories (one level via ParentCategoryID)
        AND (
            @CategoryID = 0
            OR im.CategoryID = @CategoryID
            OR im.CategoryID IN (
                SELECT ID FROM InventoryCategory
                WHERE ParentCategoryID = @CategoryID
            )
        )

        -- Size filter: NULL = all (incl. items with NULL Size)
        --             value set = exact match only (NULL-size items excluded)
        AND (@Size IS NULL OR v.Size = @Size)

    ORDER BY
        v.ItemName        ASC,
        v.TransactionDate ASC,
        v.ID              ASC;
END
GO

PRINT 'sp_InventoryStockLedger: Refreshed with CategoryID and Size parameters.';

PRINT '';
PRINT '============================================================';
PRINT 'Migration 48: Stock Ledger Category/Size Filters - Completed.';
PRINT '============================================================';

-- ─────────────────────────────────────────────────────────────
-- VERIFICATION QUERIES (run manually after executing):
--
-- 1. Get sizes for category ID 5 (adjust ID as needed):
--    EXEC sp_GetItemSizesByCategory @CategoryID = 5;
--
-- 2. Get sizes across all categories:
--    EXEC sp_GetItemSizesByCategory @CategoryID = 0;
--
-- 3. Stock ledger filtered by category + size:
--    EXEC sp_InventoryStockLedger
--        @WarehouseID = 1,
--        @FromDate    = '2026-01-01',
--        @ToDate      = '2026-12-31',
--        @CategoryID  = 5,
--        @Size        = '30';
--
-- 4. Verify existing call (no new params) still works:
--    EXEC sp_InventoryStockLedger
--        @WarehouseID = 1,
--        @FromDate    = '2026-01-01',
--        @ToDate      = '2026-12-31';
-- ─────────────────────────────────────────────────────────────
