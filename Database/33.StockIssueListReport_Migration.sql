-- ============================================================
-- Migration 33: Stock Issue List Report
-- Creates sp_StockIssueListReport stored procedure
-- and registers the screen in ScreenList for access control.
--
-- This report lists Stock Issue transactions (detail lines)
-- filtered by date range, warehouse, issue-to type, and item.
-- Each row = one StockIssueDocDetail line with header info.
--
-- Table reference:
--   StockIssueDoc       : ID, IssueNo, IssueDate, WarehouseID,
--                         IssueToType, IssueToID, IssueToName, Remarks
--   StockIssueDocDetail : ID, IssueDocID, ItemID, UOMID, IssueQty, UnitCost
-- ============================================================

USE [OBMSUATNEW]
GO

-- ── 1. Drop existing SP if it exists ─────────────────────────────────────
IF OBJECT_ID('dbo.sp_StockIssueListReport', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_StockIssueListReport;
GO

-- ── 2. Create stored procedure ────────────────────────────────────────────
CREATE PROCEDURE dbo.sp_StockIssueListReport
    @WarehouseID  INT           = 0,
    @IssueToType  NVARCHAR(20)  = 'ALL',   -- ALL | Employee | Guard | Branch | Other
    @ItemID       INT           = 0,
    @FromDate     DATETIME      = NULL,
    @ToDate       DATETIME      = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Extend ToDate to end of day (23:59:59)
    SET @ToDate = DATEADD(SECOND, 86399, CAST(CAST(@ToDate AS DATE) AS DATETIME));

    SELECT
        -- Issue document header
        s.ID                                                AS IssueDocID,
        s.IssueNo,
        CONVERT(VARCHAR(20), s.IssueDate, 106)              AS IssueDate,
        s.IssueDate                                         AS IssueDateRaw,
        ISNULL(wh.Name, '')                                 AS WarehouseName,
        ISNULL(s.IssueToType, '')                           AS IssueToType,
        ISNULL(s.IssueToName, '')                           AS IssueToName,
        ISNULL(s.Remarks, '')                               AS Remarks,
        ISNULL(s.LastUpdatedBy, '')                         AS PreparedBy,

        -- Item detail
        im.ID                                               AS ItemID,
        im.Code                                             AS ItemCode,
        im.Name                                             AS ItemName,
        ISNULL(ic.Name, '')                                 AS CategoryName,
        ISNULL(bm.Name, '')                                 AS BrandName,
        ISNULL(um.Name, '')                                 AS UOMName,

        -- Quantities & cost
        ISNULL(d.IssueQty, 0)                               AS IssueQty,
        ISNULL(d.UnitCost, 0)                               AS UnitCost,
        ISNULL(d.IssueQty, 0) * ISNULL(d.UnitCost, 0)      AS LineTotal

    FROM StockIssueDoc        s
        INNER JOIN StockIssueDocDetail d   ON d.IssueDocID  = s.ID
        INNER JOIN WarehouseMaster     wh  ON s.WarehouseID = wh.ID
        INNER JOIN ItemMaster          im  ON d.ItemID      = im.ID
        LEFT  JOIN InventoryCategory   ic  ON im.CategoryID = ic.ID
        LEFT  JOIN BrandMaster         bm  ON im.BrandID    = bm.ID
        LEFT  JOIN UOMMaster           um  ON d.UOMID       = um.ID
    WHERE s.IsActive = 1
      AND s.IssueDate BETWEEN @FromDate AND @ToDate
      AND (@WarehouseID = 0    OR s.WarehouseID = @WarehouseID)
      AND (@IssueToType = 'ALL' OR s.IssueToType = @IssueToType)
      AND (@ItemID      = 0    OR d.ItemID       = @ItemID)
    ORDER BY
        s.IssueDate ASC,
        s.IssueNo   ASC,
        im.Name     ASC;
END
GO

-- ── 3. Register screen for access control ────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Issue List Report'
)
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Issue List Report', 'Inventory', GETDATE(), 'MIGRATION');
END
GO

PRINT 'Migration 33: sp_StockIssueListReport and ScreenList entry created successfully.';
GO
