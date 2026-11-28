-- ============================================================
-- Migration 51: Stock Movement Report — Fix ClientName column
--               + add @ClientName filter parameter to SP
--
-- Problem: sp_StockMovementReport did not return ClientName
--          because @ClientName param was missing — ASPX was
--          passing the param but SP rejected it silently.
--
-- Fix: Recreate SP with @ClientName optional param.
--      '' / NULL = All clients (no filter).
--      When set  = only rows with that client's ISSUE txns.
--
-- IDEMPOTENT: DROP + CREATE. Safe to re-run.
-- Target DB  : OBMSUATNEW
-- ============================================================

PRINT '============================================================';
PRINT 'Migration 51: sp_StockMovementReport ClientName fix - Start';
PRINT '============================================================';

IF OBJECT_ID('dbo.sp_StockMovementReport', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_StockMovementReport;
    PRINT 'sp_StockMovementReport: Existing procedure dropped.';
END
GO

CREATE PROCEDURE dbo.sp_StockMovementReport
    @WarehouseID INT            = 0,
    @ItemID      INT            = 0,
    @FromDate    DATETIME       = NULL,
    @ToDate      DATETIME       = NULL,
    @ClientName  NVARCHAR(200)  = ''       -- '' / NULL = All clients
AS
BEGIN
    SET NOCOUNT ON;

    -- Normalize empty string to NULL for cleaner filter comparison
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
        -- One distinct client  → show name.
        -- Multiple clients     → "Multiple".
        -- No ISSUE transactions → blank.
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

        -- Last Unit Cost (most recent cost on or before ToDate)
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
        -- Join StockIssues to resolve Branch (client name) for ISSUE txns
        LEFT  JOIN StockIssues       si ON sl.TransactionType = 'ISSUE'
                                       AND sl.ReferenceID    = si.ID

    WHERE wh.IsActive = 1
      AND (@WarehouseID = 0 OR sl.WarehouseID = @WarehouseID)
      AND (@ItemID      = 0 OR sl.ItemID      = @ItemID)

      -- @ClientName filter: when set, include only warehouse+item combos
      -- that have at least one ISSUE transaction for this client in the period
      AND (
          @ClientName IS NULL
          OR EXISTS (
              SELECT 1
              FROM   StockLedger    sl_c
              INNER JOIN StockIssues si_c ON sl_c.ReferenceID = si_c.ID
              WHERE  sl_c.WarehouseID      = sl.WarehouseID
                AND  sl_c.ItemID           = sl.ItemID
                AND  sl_c.TransactionType  = 'ISSUE'
                AND  sl_c.TransactionDate  BETWEEN @FromDate AND @ToDate
                AND  si_c.Branch           = @ClientName
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

PRINT 'sp_StockMovementReport: Recreated with @ClientName param + ClientName column.';
PRINT '';
PRINT '============================================================';
PRINT 'Migration 51: sp_StockMovementReport ClientName fix - Done.';
PRINT '============================================================';

-- VERIFY:
-- All clients:
-- EXEC sp_StockMovementReport @WarehouseID=0, @FromDate='2026-01-01', @ToDate='2026-12-31';
-- Specific client:
-- EXEC sp_StockMovementReport @WarehouseID=0, @FromDate='2026-01-01', @ToDate='2026-12-31', @ClientName='FWG PATROL SDN BHD';
