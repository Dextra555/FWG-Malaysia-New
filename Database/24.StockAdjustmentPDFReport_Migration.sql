-- ============================================================
-- OBMS Migration 24 — Stock Adjustment PDF Report SP
--
-- sp_Report_StockAdjustment
--   @AdjustmentID INT
--
-- Single result set — StockAdjustment is a single-line entity
-- (one item per adjustment, no detail table).
--
-- Joins:
--   StockAdjustment
--     JOIN  WarehouseMaster
--     JOIN  ItemMaster
--     LEFT  JOIN InventoryCategory
--     LEFT  JOIN UOMMaster
--
-- Read-only: does NOT modify stock ledger, balances,
-- ADJ_PLUS/ADJ_MINUS entries, or any other record.
-- IDEMPOTENT — DROP + CREATE, safe to re-run.
-- ============================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '============================================================';
PRINT 'Migration 24 — sp_Report_StockAdjustment — Starting...';
PRINT '============================================================';

IF OBJECT_ID('sp_Report_StockAdjustment', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_StockAdjustment;
GO

CREATE PROCEDURE sp_Report_StockAdjustment
    @AdjustmentID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Single result set ───────────────────────────────────────────────────
    -- All column names confirmed against:
    --   StockAdjustment (Migration 04 Section 7 + Migration 16 approval columns)
    --   StockAdjustmentListDto (repository GetStockAdjustmentByID)
    SELECT
        sa.ID,
        sa.AdjustmentNo,
        CONVERT(VARCHAR(20), sa.AdjustmentDate, 106)  AS AdjustmentDate,
        wm.Name                                       AS WarehouseName,
        im.Code                                       AS ItemCode,
        im.Name                                       AS ItemName,
        ISNULL(ic.Name, '')                           AS CategoryName,
        ISNULL(um.Name, '')                           AS UOMName,
        sa.AdjustmentType,                             -- PLUS | MINUS
        sa.AdjustmentQty,
        sa.UnitCost,
        (sa.AdjustmentQty * sa.UnitCost)              AS TotalValue,
        sa.Reason,
        ISNULL(sa.Remarks, '')                       AS Remarks,
        sa.Status,                                    -- Draft | Submitted | Approved | Rejected
        ISNULL(sa.SubmittedBy, '')                   AS SubmittedBy,
        ISNULL(sa.ApprovedBy, '')                    AS ApprovedBy,
        CONVERT(VARCHAR(20), sa.ApprovedDate, 106)   AS ApprovedDate,
        ISNULL(sa.ApprovalRemarks, '')               AS ApprovalRemarks,
        ISNULL(sa.LastUpdatedBy, '')                 AS PreparedBy
    FROM  StockAdjustment sa
    INNER JOIN WarehouseMaster   wm ON wm.ID = sa.WarehouseID
    INNER JOIN ItemMaster         im ON im.ID = sa.ItemID
    LEFT  JOIN InventoryCategory ic ON ic.ID = im.CategoryID
    LEFT  JOIN UOMMaster         um ON um.ID = im.UOMID
    WHERE sa.ID       = @AdjustmentID
      AND sa.IsActive = 1;
END
GO

PRINT 'sp_Report_StockAdjustment created.';
PRINT '============================================================';
PRINT 'Migration 24 complete.';
PRINT '============================================================';
