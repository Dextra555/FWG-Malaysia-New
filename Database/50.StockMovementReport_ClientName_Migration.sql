-- ============================================================
-- Migration 50: Stock Movement Report — Add Client Name,
--               Client Name filter & fix ClosingBalance total
--
-- Changes to sp_StockMovementReport:
--   1. Add @ClientName NVARCHAR(200) = '' — optional filter
--      When set, only rows where StockIssues.Branch matches
--      are returned.
--   2. Add ClientName column in SELECT (as before).
--
-- IDEMPOTENT: DROP + CREATE. Safe to re-run.
-- Target DB  : OBMSUATNEW
-- ============================================================

PRINT '============================================================';
PRINT 'Migration 50: Stock Movement ClientName filter - Starting...';
PRINT '============================================================';

IF OBJECT_ID('dbo.sp_StockMovementReport', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_StockMovementReport;
    PRINT 'sp_StockMovementReport: Existing procedure dropped.';
END
GO

CREATE PROCEDURE dbo.sp_StockMovementReport
    @WarehouseID INT             = 0,
    @ItemID      INT             = 0,
    @FromDate    DATETIME        = NULL,
    @ToDate      DATETIME        = NULL,
    @ClientName  NVARCHAR(200)   = ''      -- '' = All clients
AS
BEGIN
    SET NOCOUNT ON;

    -- Normalize empty string to NULL for cleaner comparison
    IF @ClientName = '' SET @ClientName = NULL;

    -- Extend ToDate to end of day (23:59:59)
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    SELECT
        wh.ID                                               AS WarehouseID,
        wh.Name                                             AS WarehouseName,
        im.ID                                               AS ItemID,
        im.Code                                             AS ItemCode,
        im.Name                                             AS ItemName,
        ISNULL(ic.Name, '')                                 AS CategoryName,
        ISNULL(bm.Name, '')                                 AS BrandName,
        ISNULL(um.Name, '')                                 AS UOMName,

        -- Client Name: from StockIssues.Branch via ISSUE transactions
        -- One distinct client → show name. Multiple → "Multiple". None → blank.
        CASE
            WHEN COUNT(DISTINCT
                CASE WHEN sl.TransactionType = 'ISSUE'
                          AND sl.TransactionDate BETWEEN @FromDate AND @ToDate
                          AND sl.ReferenceID IS NOT NULL
                     THEN si.Branch END
            ) = 0 THEN ''
            WHEN COUNT(DISTINCT
                CASE WHEN sl.TransactionType = 'ISSUE'
                          AND sl.TransactionDate BETWEEN @FromDate AND @ToDate
                          AND sl.ReferenceID IS NOT NULL
                     THEN si.Branch END
            ) = 1 THEN MAX(
                CASE WHEN sl.TransactionType = 'ISSUE'
                          AND sl.TransactionDate BETWEEN @FromDate AND @ToDate
                          AND sl.ReferenceID IS NOT NULL
                     THEN si.Branch END
            )
            ELSE 'Multiple'
        END                                                 AS ClientName,

        -- Opening Balance: all movements BEFORE @FromDate
        ISNULL((
            SELECT SUM(sl_open.InQty - sl_open.OutQty)
            FROM   StockLedger sl_open
            WHERE  sl_open.WarehouseID = sl.WarehouseID
              AND  sl_open.ItemID      = sl.ItemID
              AND  sl_open.TransactionDate < @FromDate
        ), 0)                                               AS OpeningBalance,

        SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                 THEN sl.InQty ELSE 0 END)                 AS TotalIn,

        SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                 THEN sl.OutQty ELSE 0 END)                AS TotalOut,

        ISNULL((
            SELECT SUM(sl_open.InQty - sl_open.OutQty)
            FROM   StockLedger sl_open
            WHERE  sl_open.WarehouseID = sl.WarehouseID
              AND  sl_open.ItemID      = sl.ItemID
              AND  sl_open.TransactionDate < @FromDate
        ), 0)
        + SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                   THEN sl.InQty ELSE 0 END)
        - SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                   THEN sl.OutQty ELSE 0 END)              AS ClosingBalance,

        (
            SELECT TOP 1 sl2.UnitCost
            FROM   StockLedger sl2
            WHERE  sl2.WarehouseID = sl.WarehouseID
              AND  sl2.ItemID      = sl.ItemID
              AND  sl2.UnitCost IS NOT NULL
              AND  sl2.UnitCost    > 0
              AND  sl2.TransactionDate <= @ToDate
            ORDER BY sl2.TransactionDate DESC, sl2.ID DESC
        )                                                   AS LastUnitCost,

        COUNT(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                   THEN 1 END)                             AS TxnCount

    FROM StockLedger sl
        INNER JOIN WarehouseMaster   wh ON sl.WarehouseID  = wh.ID
        INNER JOIN ItemMaster        im ON sl.ItemID       = im.ID
        LEFT  JOIN InventoryCategory ic ON im.CategoryID   = ic.ID
        LEFT  JOIN BrandMaster       bm ON im.BrandID      = bm.ID
        LEFT  JOIN UOMMaster         um ON im.UOMID        = um.ID
        LEFT  JOIN StockIssues       si ON sl.TransactionType = 'ISSUE'
                                       AND sl.ReferenceID   = si.ID

    WHERE wh.IsActive = 1
      AND (@WarehouseID = 0   OR sl.WarehouseID = @WarehouseID)
      AND (@ItemID      = 0   OR sl.ItemID      = @ItemID)

      -- Client Name filter: when set, include only warehouse+item combos
      -- that have at least one ISSUE transaction matching this client
      AND (
          @ClientName IS NULL
          OR EXISTS (
              SELECT 1
              FROM   StockLedger sl_c
              INNER JOIN StockIssues si_c
                  ON sl_c.ReferenceID = si_c.ID
              WHERE  sl_c.WarehouseID    = sl.WarehouseID
                AND  sl_c.ItemID        = sl.ItemID
                AND  sl_c.TransactionType = 'ISSUE'
                AND  sl_c.TransactionDate BETWEEN @FromDate AND @ToDate
                AND  si_c.Branch        = @ClientName
          )
      )

    GROUP BY
        sl.WarehouseID, sl.ItemID,
        wh.ID, wh.Name,
        im.ID, im.Code, im.Name, im.ReorderLevel,
        ic.Name, bm.Name, um.Name

    HAVING
        SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                 THEN sl.InQty + sl.OutQty ELSE 0 END) > 0
        OR
        ISNULL((
            SELECT SUM(sl_open.InQty - sl_open.OutQty)
            FROM   StockLedger sl_open
            WHERE  sl_open.WarehouseID = sl.WarehouseID
              AND  sl_open.ItemID      = sl.ItemID
              AND  sl_open.TransactionDate < @FromDate
        ), 0) <> 0

    ORDER BY
        wh.Name ASC,
        im.Name ASC;
END
GO

PRINT 'sp_StockMovementReport: Refreshed with @ClientName filter.';
PRINT '';
PRINT '============================================================';
PRINT 'Migration 50: Stock Movement ClientName filter - Completed.';
PRINT '============================================================';

-- VERIFY:
-- EXEC sp_StockMovementReport @WarehouseID=1,
--      @FromDate='2026-01-01', @ToDate='2026-12-31',
--      @ClientName='FWG PATROL SDN BHD';   -- replace with actual branch name
-- EXEC sp_StockMovementReport @WarehouseID=1,
--      @FromDate='2026-01-01', @ToDate='2026-12-31'; -- all clients unchanged


IF OBJECT_ID('dbo.sp_StockMovementReport', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_StockMovementReport;
    PRINT 'sp_StockMovementReport: Existing procedure dropped.';
END
GO

CREATE PROCEDURE dbo.sp_StockMovementReport
    @WarehouseID INT      = 0,
    @ItemID      INT      = 0,
    @FromDate    DATETIME = NULL,
    @ToDate      DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Extend ToDate to end of day (23:59:59)
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    SELECT
        wh.ID                                               AS WarehouseID,
        wh.Name                                             AS WarehouseName,
        im.ID                                               AS ItemID,
        im.Code                                             AS ItemCode,
        im.Name                                             AS ItemName,
        ISNULL(ic.Name, '')                                 AS CategoryName,
        ISNULL(bm.Name, '')                                 AS BrandName,
        ISNULL(um.Name, '')                                 AS UOMName,

        -- ── Client Name: from StockIssues.Branch via ISSUE transactions ──
        -- If only one distinct client issued this item → show client name.
        -- If multiple clients → show "Multiple".
        -- If no ISSUE transactions → blank.
        CASE
            WHEN COUNT(DISTINCT
                CASE WHEN sl.TransactionType = 'ISSUE'
                          AND sl.TransactionDate BETWEEN @FromDate AND @ToDate
                          AND sl.ReferenceID IS NOT NULL
                     THEN si.Branch
                END
            ) = 0 THEN ''
            WHEN COUNT(DISTINCT
                CASE WHEN sl.TransactionType = 'ISSUE'
                          AND sl.TransactionDate BETWEEN @FromDate AND @ToDate
                          AND sl.ReferenceID IS NOT NULL
                     THEN si.Branch
                END
            ) = 1 THEN MAX(
                CASE WHEN sl.TransactionType = 'ISSUE'
                          AND sl.TransactionDate BETWEEN @FromDate AND @ToDate
                          AND sl.ReferenceID IS NOT NULL
                     THEN si.Branch
                END
            )
            ELSE 'Multiple'
        END                                                 AS ClientName,

        -- Opening Balance: all movements BEFORE @FromDate
        ISNULL((
            SELECT SUM(sl_open.InQty - sl_open.OutQty)
            FROM   StockLedger sl_open
            WHERE  sl_open.WarehouseID = sl.WarehouseID
              AND  sl_open.ItemID      = sl.ItemID
              AND  sl_open.TransactionDate < @FromDate
        ), 0)                                               AS OpeningBalance,

        -- Total IN within period
        SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                 THEN sl.InQty ELSE 0 END)                 AS TotalIn,

        -- Total OUT within period
        SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                 THEN sl.OutQty ELSE 0 END)                AS TotalOut,

        -- Closing Balance: OpeningBalance + TotalIn - TotalOut
        ISNULL((
            SELECT SUM(sl_open.InQty - sl_open.OutQty)
            FROM   StockLedger sl_open
            WHERE  sl_open.WarehouseID = sl.WarehouseID
              AND  sl_open.ItemID      = sl.ItemID
              AND  sl_open.TransactionDate < @FromDate
        ), 0)
        + SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                   THEN sl.InQty ELSE 0 END)
        - SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                   THEN sl.OutQty ELSE 0 END)              AS ClosingBalance,

        -- Last Unit Cost
        (
            SELECT TOP 1 sl2.UnitCost
            FROM   StockLedger sl2
            WHERE  sl2.WarehouseID = sl.WarehouseID
              AND  sl2.ItemID      = sl.ItemID
              AND  sl2.UnitCost IS NOT NULL
              AND  sl2.UnitCost    > 0
              AND  sl2.TransactionDate <= @ToDate
            ORDER BY sl2.TransactionDate DESC, sl2.ID DESC
        )                                                   AS LastUnitCost,

        -- Transaction count within period
        COUNT(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                   THEN 1 END)                             AS TxnCount

    FROM StockLedger sl
        INNER JOIN WarehouseMaster   wh ON sl.WarehouseID  = wh.ID
        INNER JOIN ItemMaster        im ON sl.ItemID       = im.ID
        LEFT  JOIN InventoryCategory ic ON im.CategoryID   = ic.ID
        LEFT  JOIN BrandMaster       bm ON im.BrandID      = bm.ID
        LEFT  JOIN UOMMaster         um ON im.UOMID        = um.ID
        -- Join StockIssues to get Branch (client name) for ISSUE transactions
        LEFT  JOIN StockIssues       si ON sl.TransactionType = 'ISSUE'
                                       AND sl.ReferenceID   = si.ID

    WHERE wh.IsActive = 1
      AND (@WarehouseID = 0 OR sl.WarehouseID = @WarehouseID)
      AND (@ItemID      = 0 OR sl.ItemID      = @ItemID)

    GROUP BY
        sl.WarehouseID, sl.ItemID,
        wh.ID, wh.Name,
        im.ID, im.Code, im.Name, im.ReorderLevel,
        ic.Name, bm.Name, um.Name

    HAVING
        SUM(CASE WHEN sl.TransactionDate BETWEEN @FromDate AND @ToDate
                 THEN sl.InQty + sl.OutQty ELSE 0 END) > 0
        OR
        ISNULL((
            SELECT SUM(sl_open.InQty - sl_open.OutQty)
            FROM   StockLedger sl_open
            WHERE  sl_open.WarehouseID = sl.WarehouseID
              AND  sl_open.ItemID      = sl.ItemID
              AND  sl_open.TransactionDate < @FromDate
        ), 0) <> 0

    ORDER BY
        wh.Name ASC,
        im.Name ASC;
END
GO

PRINT 'sp_StockMovementReport: Refreshed with ClientName column.';
PRINT '';
PRINT '============================================================';
PRINT 'Migration 50: Stock Movement ClientName - Completed.';
PRINT '============================================================';

-- VERIFY:
-- EXEC sp_StockMovementReport @WarehouseID=1, @FromDate='2026-01-01', @ToDate='2026-12-31';
