-- ============================================================
-- OBMS Inventory Module — Clear / Reset Data Script
-- File  : 51.Inventory_ClearData_Script.sql
--
-- Clears ALL transaction data for:
--   Opening Stock, Purchase Requisition, Purchase Order,
--   Goods Receipt, Purchase Return, Stock Operations,
--   Branch Stock Request, Stock Transfer, Stock Issue,
--   Issue Return, Stock Adjustment, Damage/Loss,
--   Asset Management (all pages)
--
-- ⚠️  PERMANENTLY DELETES DATA — run on DEV/UAT only.
-- ============================================================

SET NOCOUNT ON;

BEGIN TRANSACTION;
BEGIN TRY

    -- =========================================================
    -- SECTION 1 : Approval & Notification
    -- =========================================================
    DELETE FROM AppNotification;
    PRINT 'AppNotification          : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM ApprovalHistory;
    PRINT 'ApprovalHistory          : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    -- =========================================================
    -- SECTION 2 : Asset Management
    -- =========================================================
    DELETE FROM AssetLocationLog;
    PRINT 'AssetLocationLog         : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM AssetMovementHistory;
    PRINT 'AssetMovementHistory     : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM AssetDamage;
    PRINT 'AssetDamage              : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM AssetMaintenance;
    PRINT 'AssetMaintenance         : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM AssetDisposal;
    PRINT 'AssetDisposal            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM AssetTransfer;
    PRINT 'AssetTransfer            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM AssetReturn;
    PRINT 'AssetReturn              : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM AssetAllocation;
    PRINT 'AssetAllocation          : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM AssetMaster;
    PRINT 'AssetMaster              : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    -- =========================================================
    -- SECTION 3 : Stock Issue & Issue Return
    -- =========================================================
    DELETE FROM StockIssueUniformLoanLink;
    PRINT 'StockIssueUniformLoanLink: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM StockIssueReturnDetail;
    PRINT 'StockIssueReturnDetail   : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM StockIssueReturn;
    PRINT 'StockIssueReturn         : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM StockIssueDocDetail;
    PRINT 'StockIssueDocDetail      : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM StockIssueDoc;
    PRINT 'StockIssueDoc            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    -- =========================================================
    -- SECTION 4 : Branch Stock Request
    -- NULL the FK link to StockTransfer first
    -- =========================================================
    DELETE FROM StockRequestDetail;
    PRINT 'StockRequestDetail       : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    UPDATE StockRequest SET LinkedTransferID = NULL WHERE LinkedTransferID IS NOT NULL;

    DELETE FROM StockRequest;
    PRINT 'StockRequest             : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    -- =========================================================
    -- SECTION 5 : Stock Transfer
    -- =========================================================
    DELETE FROM StockTransferDetail;
    PRINT 'StockTransferDetail      : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM StockTransfer;
    PRINT 'StockTransfer            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    -- =========================================================
    -- SECTION 6 : Stock Adjustment & Damage / Loss
    -- =========================================================
    DELETE FROM StockAdjustment;
    PRINT 'StockAdjustment          : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM StockDamageLoss;
    PRINT 'StockDamageLoss          : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    -- =========================================================
    -- SECTION 7 : Purchase Flow  PR → PO → GRN → Return
    -- =========================================================
    DELETE FROM PurchaseReturnDetail;
    PRINT 'PurchaseReturnDetail     : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM PurchaseReturn;
    PRINT 'PurchaseReturn           : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM GoodsReceiptNoteDetail;
    PRINT 'GoodsReceiptNoteDetail   : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    -- NULL the Finance-side FK before deleting GRN rows
    -- (BranchPaymentDetails.GRNID → GoodsReceiptNote.ID)
    UPDATE BranchPaymentDetails SET GRNID = NULL WHERE GRNID IS NOT NULL;
    PRINT 'BranchPaymentDetails.GRNID nulled out';

    DELETE FROM GoodsReceiptNote;
    PRINT 'GoodsReceiptNote         : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM PurchaseOrderDetail;
    PRINT 'PurchaseOrderDetail      : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM PurchaseOrder;
    PRINT 'PurchaseOrder            : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM PRProcessingTask;
    PRINT 'PRProcessingTask         : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM PurchaseRequisitionDetail;
    PRINT 'PurchaseRequisitionDetail: ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM PurchaseRequisition;
    PRINT 'PurchaseRequisition      : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    -- =========================================================
    -- SECTION 8 : Opening Stock & StockLedger
    -- =========================================================
    DELETE FROM OpeningStock;
    PRINT 'OpeningStock             : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';

    DELETE FROM StockLedger;
    PRINT 'StockLedger              : ' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows deleted';


    -- =========================================================
    -- OPTIONAL — Reset Identity Seeds (uncomment if needed)
    -- =========================================================
    /*
    DBCC CHECKIDENT ('AppNotification',           RESEED, 0);
    DBCC CHECKIDENT ('ApprovalHistory',           RESEED, 0);
    DBCC CHECKIDENT ('AssetLocationLog',          RESEED, 0);
    DBCC CHECKIDENT ('AssetMovementHistory',      RESEED, 0);
    DBCC CHECKIDENT ('AssetDamage',               RESEED, 0);
    DBCC CHECKIDENT ('AssetMaintenance',          RESEED, 0);
    DBCC CHECKIDENT ('AssetDisposal',             RESEED, 0);
    DBCC CHECKIDENT ('AssetTransfer',             RESEED, 0);
    DBCC CHECKIDENT ('AssetReturn',               RESEED, 0);
    DBCC CHECKIDENT ('AssetAllocation',           RESEED, 0);
    DBCC CHECKIDENT ('AssetMaster',               RESEED, 0);
    DBCC CHECKIDENT ('StockIssueUniformLoanLink', RESEED, 0);
    DBCC CHECKIDENT ('StockIssueReturnDetail',    RESEED, 0);
    DBCC CHECKIDENT ('StockIssueReturn',          RESEED, 0);
    DBCC CHECKIDENT ('StockIssueDocDetail',       RESEED, 0);
    DBCC CHECKIDENT ('StockIssueDoc',             RESEED, 0);
    DBCC CHECKIDENT ('StockRequestDetail',        RESEED, 0);
    DBCC CHECKIDENT ('StockRequest',              RESEED, 0);
    DBCC CHECKIDENT ('StockTransferDetail',       RESEED, 0);
    DBCC CHECKIDENT ('StockTransfer',             RESEED, 0);
    DBCC CHECKIDENT ('StockAdjustment',           RESEED, 0);
    DBCC CHECKIDENT ('StockDamageLoss',           RESEED, 0);
    DBCC CHECKIDENT ('PurchaseReturnDetail',      RESEED, 0);
    DBCC CHECKIDENT ('PurchaseReturn',            RESEED, 0);
    DBCC CHECKIDENT ('GoodsReceiptNoteDetail',    RESEED, 0);
    DBCC CHECKIDENT ('GoodsReceiptNote',          RESEED, 0);
    DBCC CHECKIDENT ('PurchaseOrderDetail',       RESEED, 0);
    DBCC CHECKIDENT ('PurchaseOrder',             RESEED, 0);
    DBCC CHECKIDENT ('PRProcessingTask',          RESEED, 0);
    DBCC CHECKIDENT ('PurchaseRequisitionDetail', RESEED, 0);
    DBCC CHECKIDENT ('PurchaseRequisition',       RESEED, 0);
    DBCC CHECKIDENT ('OpeningStock',              RESEED, 0);
    DBCC CHECKIDENT ('StockLedger',               RESEED, 0);
    */

    -- =========================================================
    COMMIT TRANSACTION;
    PRINT '============================================================';
    PRINT 'Done. All Inventory data cleared successfully.';
    PRINT '============================================================';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '============================================================';
    PRINT 'ERROR — rolled back. Nothing was deleted.';
    PRINT 'Msg   : ' + ERROR_MESSAGE();
    PRINT 'Line  : ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT '============================================================';
    THROW;
END CATCH;
