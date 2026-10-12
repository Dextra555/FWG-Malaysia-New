-- ============================================================
-- OBMS Migration 22 — Stock Issue Return PDF Report SP
--
-- sp_Report_StockIssueReturn
--   @IssueReturnID INT
--
-- Result Set 1 : document header
--   StockIssueReturn
--     JOIN StockIssueDoc   (to get OriginalIssueNo + IssueToName)
--     JOIN WarehouseMaster (to get WarehouseName)
--
-- Result Set 2 : document detail rows
--   StockIssueReturnDetail
--     JOIN  ItemMaster
--     LEFT  JOIN InventoryCategory
--     LEFT  JOIN UOMMaster
--
-- Read-only: does NOT modify stock ledger, balances, or any record.
-- IDEMPOTENT — DROP + CREATE, safe to re-run.
-- ============================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '============================================================';
PRINT 'Migration 22 — sp_Report_StockIssueReturn — Starting...';
PRINT '============================================================';

IF OBJECT_ID('sp_Report_StockIssueReturn', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_StockIssueReturn;
GO

CREATE PROCEDURE sp_Report_StockIssueReturn
    @IssueReturnID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Result Set 1 : Header ────────────────────────────────────────────────
    -- All column names confirmed against StockIssueReturn and StockIssueDoc
    -- tables from Migration 04 and the StockIssueReturnListDto DTO.
    SELECT
        sir.ID,
        sir.ReturnNo,
        CONVERT(VARCHAR(20), sir.ReturnDate, 106)       AS ReturnDate,
        sid.IssueNo                                     AS OriginalIssueNo,
        wm.Name                                         AS WarehouseName,
        ISNULL(sid.IssueToName, '')                     AS IssueToName,
        ISNULL(sir.Remarks, '')                         AS Remarks,
        ISNULL(sir.LastUpdatedBy, '')                   AS PreparedBy,
        CONVERT(VARCHAR(20), sir.LASTUPDATE, 106)       AS LastUpdateDate
    FROM  StockIssueReturn  sir
    INNER JOIN StockIssueDoc    sid ON sid.ID = sir.OriginalIssueDocID
    INNER JOIN WarehouseMaster  wm  ON wm.ID  = sir.WarehouseID
    WHERE sir.ID       = @IssueReturnID
      AND sir.IsActive = 1;

    -- ── Result Set 2 : Detail rows ───────────────────────────────────────────
    -- All column names confirmed against StockIssueReturnDetail (Migration 04)
    -- and StockIssueReturnDetailListDto.
    SELECT
        im.Code                                         AS ItemCode,
        im.Name                                         AS ItemName,
        ISNULL(ic.Name, '')                             AS CategoryName,
        ISNULL(um.Name, '')                             AS UOMName,
        d.ReturnQty,
        d.UnitCost,
        (d.ReturnQty * d.UnitCost)                      AS LineTotal,
        ISNULL(d.Remarks, '')                           AS Remarks
    FROM  StockIssueReturnDetail d
    INNER JOIN ItemMaster        im ON im.ID = d.ItemID
    LEFT  JOIN InventoryCategory ic ON ic.ID = im.CategoryID
    LEFT  JOIN UOMMaster         um ON um.ID = d.UOMID
    WHERE d.ReturnID = @IssueReturnID
    ORDER BY im.Name, im.Code;
END
GO

PRINT 'sp_Report_StockIssueReturn created.';
PRINT '============================================================';
PRINT 'Migration 22 complete.';
PRINT '============================================================';
