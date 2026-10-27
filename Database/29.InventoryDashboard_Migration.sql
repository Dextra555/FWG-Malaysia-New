-- ============================================================
-- OBMS Inventory Dashboard Migration
-- Creates sp_InventoryDashboard and registers the screen.
--
-- IDEMPOTENT: SP is dropped and recreated; ScreenList entry
-- is skipped if already present.
--
-- Target database : OBMSUATNEW (or live equivalent)
-- Last updated    : 2026
--
-- Result sets returned by sp_InventoryDashboard:
--   RS1  – KPI Cards (single row)
--   RS2  – Warehouse Stock Value (donut chart)
--   RS3  – Monthly Inward vs Outward by day (bar chart)
--   RS4  – Warehouse Stock Summary (table)
--   RS5  – Low Stock Alert items (table)
--   RS6  – Pending Activities counts (single row)
--   RS7  – Recent Stock Transactions (last 20 rows)
--   RS8  – Stock Quantity by Warehouse – Top 5 Items (cross-tab)
--
-- NOTE: ItemMaster is a pre-existing table with NO IsActive
--       column. All joins to ItemMaster use plain ID match only.
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Inventory Dashboard Migration - Starting...';
PRINT '============================================================';

-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : sp_InventoryDashboard
-- ─────────────────────────────────────────────────────────────

PRINT '';
PRINT 'Section 1: Creating sp_InventoryDashboard...';

IF OBJECT_ID('sp_InventoryDashboard', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE sp_InventoryDashboard;
    PRINT 'sp_InventoryDashboard: Existing procedure dropped.';
END
GO

CREATE PROCEDURE sp_InventoryDashboard
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MonthStart DATE = DATEADD(DAY, 1 - DAY(GETDATE()), CAST(GETDATE() AS DATE));
    DECLARE @MonthEnd   DATE = CAST(GETDATE() AS DATE);

    -- ── Reusable CTEs ───────────────────────────────────────────────────────

    -- Current balance per (WarehouseID, ItemID)
    ;WITH CurrentBalance AS (
        SELECT
            sl.WarehouseID,
            sl.ItemID,
            SUM(sl.InQty - sl.OutQty) AS BalanceQty
        FROM StockLedger sl
        GROUP BY sl.WarehouseID, sl.ItemID
    ),
    -- Last non-zero unit cost per (WarehouseID, ItemID)
    LastCost AS (
        SELECT
            sl.WarehouseID,
            sl.ItemID,
            sl.UnitCost,
            ROW_NUMBER() OVER (
                PARTITION BY sl.WarehouseID, sl.ItemID
                ORDER BY sl.TransactionDate DESC, sl.ID DESC
            ) AS rn
        FROM StockLedger sl
        WHERE ISNULL(sl.UnitCost, 0) > 0
    )

    -- ══════════════════════════════════════════════════════════════════════
    -- RS1 : KPI Cards (single row)
    -- ══════════════════════════════════════════════════════════════════════
    SELECT
        -- Total Items — ItemMaster has no IsActive; count all rows
        (SELECT COUNT(*) FROM ItemMaster)
            AS TotalItems,

        -- Total Stock Value
        ISNULL((
            SELECT SUM(cb.BalanceQty * ISNULL(lc.UnitCost, 0))
            FROM CurrentBalance cb
            LEFT JOIN LastCost lc
                ON lc.WarehouseID = cb.WarehouseID
               AND lc.ItemID      = cb.ItemID
               AND lc.rn          = 1
            WHERE cb.BalanceQty > 0
        ), 0) AS TotalStockValue,

        -- Total Stock Qty (global; transfers cancel)
        ISNULL((
            SELECT SUM(BalanceQty)
            FROM CurrentBalance
            WHERE BalanceQty > 0
        ), 0) AS TotalStockQty,

        -- Pending Transfers this month
        (
            SELECT COUNT(*)
            FROM StockTransfer
            WHERE IsActive = 1
              AND TransferDate >= @MonthStart
              AND TransferDate <= @MonthEnd
        ) AS PendingTransfers,

        -- Pending Approvals: Submitted documents waiting for approval
        -- Covers PR, PO, Stock Adjustment, BSR — same as VW_PendingApprovals
        (
            SELECT
                (SELECT COUNT(*) FROM PurchaseRequisition  WHERE IsActive=1 AND Status='Submitted') +
                (SELECT COUNT(*) FROM PurchaseOrder        WHERE IsActive=1 AND Status='Submitted') +
                (SELECT COUNT(*) FROM StockAdjustment      WHERE IsActive=1 AND Status='Submitted') +
                (SELECT COUNT(*) FROM StockRequest         WHERE IsActive=1 AND Status='Submitted')
        ) AS PendingIssues,

        -- Low Stock Items (balance <= ReorderLevel, where ReorderLevel configured)
        (
            SELECT COUNT(DISTINCT cb.ItemID)
            FROM CurrentBalance cb
            INNER JOIN ItemMaster im ON im.ID = cb.ItemID
            WHERE ISNULL(im.ReorderLevel, 0) > 0
              AND cb.BalanceQty <= im.ReorderLevel
        ) AS LowStockItems;


    -- ══════════════════════════════════════════════════════════════════════
    -- RS2 : Warehouse Stock Value (donut chart)
    -- ══════════════════════════════════════════════════════════════════════
    ;WITH CurrentBalance2 AS (
        SELECT sl.WarehouseID, sl.ItemID,
               SUM(sl.InQty - sl.OutQty) AS BalanceQty
        FROM StockLedger sl
        GROUP BY sl.WarehouseID, sl.ItemID
    ),
    LastCost2 AS (
        SELECT sl.WarehouseID, sl.ItemID, sl.UnitCost,
               ROW_NUMBER() OVER (
                   PARTITION BY sl.WarehouseID, sl.ItemID
                   ORDER BY sl.TransactionDate DESC, sl.ID DESC
               ) AS rn
        FROM StockLedger sl
        WHERE ISNULL(sl.UnitCost, 0) > 0
    )
    SELECT
        wh.ID                                                               AS WarehouseID,
        wh.Name                                                             AS WarehouseName,
        ISNULL(SUM(CASE WHEN cb.BalanceQty > 0
                        THEN cb.BalanceQty * ISNULL(lc.UnitCost, 0)
                        ELSE 0 END), 0)                                     AS StockValue,
        ISNULL(SUM(CASE WHEN cb.BalanceQty > 0 THEN cb.BalanceQty ELSE 0 END), 0) AS StockQty
    FROM WarehouseMaster wh
    INNER JOIN CurrentBalance2 cb ON cb.WarehouseID = wh.ID
    LEFT  JOIN LastCost2 lc
           ON lc.WarehouseID = cb.WarehouseID
          AND lc.ItemID      = cb.ItemID
          AND lc.rn          = 1
    WHERE wh.IsActive = 1
    GROUP BY wh.ID, wh.Name
    HAVING ISNULL(SUM(CASE WHEN cb.BalanceQty > 0 THEN cb.BalanceQty ELSE 0 END), 0) > 0
    ORDER BY StockValue DESC;


    -- ══════════════════════════════════════════════════════════════════════
    -- RS3 : Monthly Inward vs Outward by day (bar chart)
    --   Groups by 5-day intervals matching the reference image (01-05, 06-10…)
    -- ══════════════════════════════════════════════════════════════════════
    ;WITH CalendarDays AS (
        SELECT @MonthStart AS CalDate
        UNION ALL
        SELECT DATEADD(DAY, 1, CalDate)
        FROM CalendarDays
        WHERE DATEADD(DAY, 1, CalDate) <= @MonthEnd
    ),
    DailyMov AS (
        SELECT
            CAST(sl.TransactionDate AS DATE) AS TxDate,
            SUM(sl.InQty)                    AS TotalIn,
            SUM(sl.OutQty)                   AS TotalOut
        FROM StockLedger sl
        WHERE CAST(sl.TransactionDate AS DATE) >= @MonthStart
          AND CAST(sl.TransactionDate AS DATE) <= @MonthEnd
        GROUP BY CAST(sl.TransactionDate AS DATE)
    ),
    -- Group into 5-day buckets
    Bucketed AS (
        SELECT
            ((DAY(cd.CalDate) - 1) / 5) * 5 + 1                         AS BucketStart,
            CASE WHEN ((DAY(cd.CalDate) - 1) / 5) * 5 + 5 >
                      DAY(EOMONTH(CAST(@MonthStart AS DATE)))
                 THEN DAY(EOMONTH(CAST(@MonthStart AS DATE)))
                 ELSE ((DAY(cd.CalDate) - 1) / 5) * 5 + 5
            END                                                          AS BucketEnd,
            ISNULL(dm.TotalIn,  0)                                       AS InQty,
            ISNULL(dm.TotalOut, 0)                                       AS OutQty
        FROM CalendarDays cd
        LEFT JOIN DailyMov dm ON dm.TxDate = cd.CalDate
    )
    SELECT
        CAST(BucketStart AS NVARCHAR(2)) + '-' +
        CAST(BucketEnd   AS NVARCHAR(2))         AS DateLabel,
        SUM(InQty)                               AS InwardQty,
        SUM(OutQty)                              AS OutwardQty
    FROM Bucketed
    GROUP BY BucketStart, BucketEnd
    ORDER BY BucketStart
    OPTION (MAXRECURSION 31);


    -- ══════════════════════════════════════════════════════════════════════
    -- RS4 : Warehouse Stock Summary (right-side panel)
    --   Returns one aggregate row across ALL warehouses
    -- ══════════════════════════════════════════════════════════════════════
    ;WITH WhsLedger AS (
        SELECT
            SUM(CASE WHEN sl.TransactionType = 'OPENING'
                     THEN sl.InQty ELSE 0 END)  AS OpeningQty,
            SUM(CASE WHEN sl.TransactionType <> 'OPENING'
                     THEN sl.InQty ELSE 0 END)  AS InwardQty,
            SUM(sl.OutQty)                      AS OutwardQty,
            SUM(sl.InQty - sl.OutQty)           AS ClosingQty
        FROM StockLedger sl
    ),
    WhsValue AS (
        SELECT ISNULL(SUM(sub.BalanceQty * ISNULL(sub.LastCost, 0)), 0) AS TotalValue
        FROM (
            SELECT sl.WarehouseID, sl.ItemID,
                   SUM(sl.InQty - sl.OutQty) AS BalanceQty,
                   MAX(CASE WHEN ISNULL(sl.UnitCost,0) > 0 THEN sl.UnitCost END) AS LastCost
            FROM StockLedger sl
            GROUP BY sl.WarehouseID, sl.ItemID
        ) sub
        WHERE sub.BalanceQty > 0
    )
    SELECT
        ISNULL(wl.OpeningQty,  0)  AS OpeningQty,
        ISNULL(wl.InwardQty,   0)  AS InwardQty,
        ISNULL(wl.OutwardQty,  0)  AS OutwardQty,
        ISNULL(wl.ClosingQty,  0)  AS ClosingQty,
        ISNULL(wv.TotalValue,  0)  AS StockValue
    FROM WhsLedger wl
    CROSS JOIN WhsValue wv;


    -- ══════════════════════════════════════════════════════════════════════
    -- RS5 : Low Stock Alert — items at or below reorder level
    --   Returns top 20 lowest (by available qty / reorder ratio)
    -- ══════════════════════════════════════════════════════════════════════
    ;WITH CurrentBalance5 AS (
        SELECT sl.WarehouseID, sl.ItemID,
               SUM(sl.InQty - sl.OutQty) AS BalanceQty
        FROM StockLedger sl
        GROUP BY sl.WarehouseID, sl.ItemID
    )
    SELECT TOP 20
        im.Code                                 AS ItemCode,
        im.Name                                 AS ItemName,
        wh.Name                                 AS WarehouseName,
        ISNULL(cb.BalanceQty, 0)                AS AvailableQty,
        im.ReorderLevel                         AS ReorderLevel,
        'Low'                                   AS [Status]
    FROM CurrentBalance5 cb
    INNER JOIN ItemMaster im      ON im.ID  = cb.ItemID
    INNER JOIN WarehouseMaster wh ON wh.ID  = cb.WarehouseID
    WHERE ISNULL(im.ReorderLevel, 0) > 0
      AND cb.BalanceQty <= im.ReorderLevel
    ORDER BY (cb.BalanceQty / im.ReorderLevel) ASC,
             cb.BalanceQty ASC;


    -- ══════════════════════════════════════════════════════════════════════
    -- RS6 : Pending Activities (single row of counts)
    -- ══════════════════════════════════════════════════════════════════════
    SELECT
        -- POs Pending: Approved PRs that do not yet have a PO created
        (
            SELECT COUNT(*)
            FROM PurchaseRequisition pr
            WHERE pr.IsActive = 1
              AND pr.Status = 'Approved'
              AND NOT EXISTS (
                  SELECT 1
                  FROM PurchaseOrder po
                  WHERE po.PRID = pr.ID
                    AND po.IsActive = 1
              )
        ) AS POsPending,

        -- GRN Pending: Approved POs that do not yet have a GRN
        -- (i.e. GRN needs to be created — true "pending" action)
        (
            SELECT COUNT(*)
            FROM PurchaseOrder po
            WHERE po.IsActive = 1
              AND po.Status = 'Approved'
              AND NOT EXISTS (
                  SELECT 1
                  FROM GoodsReceiptNote grn
                  WHERE grn.POID = po.ID
                    AND grn.IsActive = 1
              )
        ) AS GRNsPending,

        -- Stock Transfers Pending: Approved BSRs with no linked Transfer created yet
        -- LinkedTransferID = NULL or 0 means transfer not yet created
        (
            SELECT COUNT(*)
            FROM StockRequest sr
            WHERE sr.IsActive = 1
              AND sr.Status = 'Approved'
              AND (sr.LinkedTransferID IS NULL OR sr.LinkedTransferID = 0)
        ) AS TransfersPending,

        -- Issues this month
        (SELECT COUNT(*) FROM StockIssueDoc
         WHERE IsActive = 1
           AND IssueDate >= @MonthStart
           AND IssueDate <= @MonthEnd) AS IssuesPending,

        -- Stock Adjustments awaiting approval
        (SELECT COUNT(*) FROM StockAdjustment
         WHERE IsActive = 1
           AND Status IN ('Draft', 'Submitted')) AS AdjustmentsPending;


    -- ══════════════════════════════════════════════════════════════════════
    -- RS7 : Recent Stock Transactions (last 20)
    -- ══════════════════════════════════════════════════════════════════════
    SELECT TOP 20
        CAST(sl.TransactionDate AS DATE)        AS TxDate,
        CASE sl.TransactionType
            WHEN 'OPENING'      THEN 'Opening Stock'
            WHEN 'STOCK_IN'     THEN 'GRN (In)'
            WHEN 'ISSUE'        THEN 'Issue (Out)'
            WHEN 'ISSUE_RETURN' THEN 'Issue Return'
            WHEN 'TRANSFER_IN'  THEN 'Transfer In'
            WHEN 'TRANSFER_OUT' THEN 'Transfer Out'
            WHEN 'ADJ_PLUS'     THEN 'Adjustment (In)'
            WHEN 'ADJ_MINUS'    THEN 'Adjustment (Out)'
            WHEN 'PURCHASE_RTN' THEN 'Purchase Return'
            WHEN 'DAMAGE_LOSS'  THEN 'Damage / Loss'
            ELSE sl.TransactionType
        END                                     AS TxType,
        sl.ReferenceNo                          AS RefNo,
        im.Name                                 AS ItemName,
        wh.Name                                 AS WarehouseName,
        CASE WHEN sl.InQty > 0 THEN sl.InQty ELSE sl.OutQty END AS Qty,
        sl.TransactionType                      AS TxTypeCode
    FROM StockLedger sl
    INNER JOIN ItemMaster     im ON im.ID = sl.ItemID
    INNER JOIN WarehouseMaster wh ON wh.ID = sl.WarehouseID
    ORDER BY sl.TransactionDate DESC, sl.ID DESC;


    -- ══════════════════════════════════════════════════════════════════════
    -- RS8 : Stock Quantity by Warehouse – Top 5 Items (flat rows)
    --   Returns rows: ItemName, WarehouseName, Qty
    --   Angular pivots this into a cross-tab display.
    --   Only items with total stock > 0; only positive warehouse balances.
    -- ══════════════════════════════════════════════════════════════════════
    ;WITH ItemWHBalance AS (
        SELECT sl.ItemID, sl.WarehouseID,
               SUM(sl.InQty - sl.OutQty) AS BalanceQty
        FROM StockLedger sl
        GROUP BY sl.ItemID, sl.WarehouseID
    ),
    ItemTotal AS (
        SELECT ItemID,
               SUM(CASE WHEN BalanceQty > 0 THEN BalanceQty ELSE 0 END) AS TotalQty
        FROM ItemWHBalance
        GROUP BY ItemID
    ),
    Top5Items AS (
        SELECT TOP 5 ItemID, TotalQty
        FROM ItemTotal
        WHERE TotalQty > 0
        ORDER BY TotalQty DESC
    )
    SELECT
        im.Name                                                     AS ItemName,
        wh.Name                                                     AS WarehouseName,
        CASE WHEN iwb.BalanceQty > 0 THEN iwb.BalanceQty ELSE 0 END AS Qty,
        t5.TotalQty
    FROM Top5Items t5
    INNER JOIN ItemMaster      im  ON im.ID  = t5.ItemID
    INNER JOIN ItemWHBalance   iwb ON iwb.ItemID = t5.ItemID
    INNER JOIN WarehouseMaster wh  ON wh.ID  = iwb.WarehouseID
    WHERE iwb.BalanceQty > 0
    ORDER BY t5.TotalQty DESC, wh.Name;

END
GO

PRINT 'sp_InventoryDashboard: Procedure created successfully.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : SCREENLIST ENTRY
-- ─────────────────────────────────────────────────────────────

PRINT '';
PRINT 'Section 2: Registering ScreenList entry...';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Inventory Dashboard')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Inventory Dashboard', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: ''Inventory Dashboard'' added.';
END
ELSE
    PRINT 'ScreenList: ''Inventory Dashboard'' already exists. Skipping.';


PRINT '';
PRINT '============================================================';
PRINT 'OBMS Inventory Dashboard Migration - Completed Successfully.';
PRINT '============================================================';
