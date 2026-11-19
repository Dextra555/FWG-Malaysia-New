-- ============================================================
-- OBMS Migration 21 — Stock Issue PDF Report Stored Procedure
--
-- sp_Report_StockIssue
--   @IssueDocID INT
--
-- Result Set 1 : document header row
--   StockIssueDoc JOIN WarehouseMaster
--
-- Result Set 2 : document detail rows
--   StockIssueDocDetail JOIN ItemMaster
--                       LEFT JOIN InventoryCategory
--                       LEFT JOIN UOMMaster
--
-- This script is IDEMPOTENT — DROP + CREATE, safe to re-run.
-- Run against OBMSUATNEW (or live equivalent).
-- ============================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '============================================================';
PRINT 'Migration 21 — sp_Report_StockIssue — Starting...';
PRINT '============================================================';

IF OBJECT_ID('sp_Report_StockIssue', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_StockIssue;
GO

CREATE PROCEDURE sp_Report_StockIssue
    @IssueDocID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Result Set 1 : Header ────────────────────────────────────────────────
    SELECT
        sid.ID,
        sid.IssueNo,
        CONVERT(VARCHAR(20), sid.IssueDate, 106)    AS IssueDate,
        wm.Name                                     AS WarehouseName,
        sid.IssueToType,
        ISNULL(sid.IssueToName, '')                 AS IssueToName,
        ISNULL(sid.Remarks, '')                     AS Remarks,
        ISNULL(sid.LastUpdatedBy, '')               AS PreparedBy,
        CONVERT(VARCHAR(20), sid.LASTUPDATE, 106)   AS LastUpdateDate
    FROM  StockIssueDoc     sid
    INNER JOIN WarehouseMaster wm ON wm.ID = sid.WarehouseID
    WHERE sid.ID       = @IssueDocID
      AND sid.IsActive = 1;

    -- ── Result Set 2 : Detail rows ───────────────────────────────────────────
    SELECT
        im.Code                                     AS ItemCode,
        im.Name                                     AS ItemName,
        ISNULL(ic.Name, '')                         AS CategoryName,
        ISNULL(um.Name, '')                         AS UOMName,
        d.IssueQty,
        d.UnitCost,
        (d.IssueQty * d.UnitCost)                   AS LineTotal,
        ISNULL(d.Remarks, '')                       AS Remarks
    FROM  StockIssueDocDetail d
    INNER JOIN ItemMaster        im ON im.ID  = d.ItemID
    LEFT  JOIN InventoryCategory ic ON ic.ID  = im.CategoryID
    LEFT  JOIN UOMMaster         um ON um.ID  = d.UOMID
    WHERE d.IssueDocID = @IssueDocID
    ORDER BY im.Name, im.Code;
END
GO

PRINT 'sp_Report_StockIssue created.';
PRINT '============================================================';
PRINT 'Migration 21 complete.';
PRINT '============================================================';
