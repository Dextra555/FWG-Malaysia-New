-- ============================================================
-- OBMS Migration 23 — Stock Transfer PDF Report SP
--
-- sp_Report_StockTransfer
--   @TransferID INT
--
-- Result Set 1 : document header
--   StockTransfer
--     JOIN WarehouseMaster (source)
--     JOIN WarehouseMaster (destination)
--
-- Result Set 2 : document detail rows
--   StockTransferDetail
--     JOIN  ItemMaster
--     LEFT  JOIN InventoryCategory
--     LEFT  JOIN UOMMaster
--
-- Read-only: does NOT modify stock ledger, balances,
-- TRANSFER_OUT / TRANSFER_IN entries, or any other record.
-- IDEMPOTENT — DROP + CREATE, safe to re-run.
-- ============================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '============================================================';
PRINT 'Migration 23 — sp_Report_StockTransfer — Starting...';
PRINT '============================================================';

IF OBJECT_ID('sp_Report_StockTransfer', 'P') IS NOT NULL
    DROP PROCEDURE sp_Report_StockTransfer;
GO

CREATE PROCEDURE sp_Report_StockTransfer
    @TransferID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── Result Set 1 : Header ────────────────────────────────────────────────
    -- All column names confirmed against StockTransfer (Migration 04 Section 5)
    -- and StockTransferListDto DTO.
    SELECT
        st.ID,
        st.TransferNo,
        CONVERT(VARCHAR(20), st.TransferDate, 106)  AS TransferDate,
        st.SourceWarehouseID,
        sw.Name                                      AS SourceWarehouseName,
        st.DestWarehouseID,
        dw.Name                                      AS DestWarehouseName,
        ISNULL(st.Remarks, '')                       AS Remarks,
        ISNULL(st.LastUpdatedBy, '')                 AS PreparedBy,
        CONVERT(VARCHAR(20), st.LASTUPDATE, 106)     AS LastUpdateDate
    FROM  StockTransfer      st
    INNER JOIN WarehouseMaster sw ON sw.ID = st.SourceWarehouseID
    INNER JOIN WarehouseMaster dw ON dw.ID = st.DestWarehouseID
    WHERE st.ID       = @TransferID
      AND st.IsActive = 1;

    -- ── Result Set 2 : Detail rows ───────────────────────────────────────────
    -- All column names confirmed against StockTransferDetail (Migration 04
    -- Section 6) and StockTransferDetailListDto.
    SELECT
        im.Code                                         AS ItemCode,
        im.Name                                         AS ItemName,
        ISNULL(ic.Name, '')                             AS CategoryName,
        ISNULL(um.Name, '')                             AS UOMName,
        d.TransferQty,
        d.UnitCost,
        (d.TransferQty * d.UnitCost)                    AS LineTotal,
        ISNULL(d.Remarks, '')                           AS Remarks
    FROM  StockTransferDetail d
    INNER JOIN ItemMaster        im ON im.ID = d.ItemID
    LEFT  JOIN InventoryCategory ic ON ic.ID = im.CategoryID
    LEFT  JOIN UOMMaster         um ON um.ID = d.UOMID
    WHERE d.TransferID = @TransferID
    ORDER BY im.Name, im.Code;
END
GO

PRINT 'sp_Report_StockTransfer created.';
PRINT '============================================================';
PRINT 'Migration 23 complete.';
PRINT '============================================================';
