-- ============================================================
-- OBMS Inventory Part 4 Migration
-- Stock Operations:
--   A. Stock Issue (Warehouse → Employee/Guard/Branch/Other)
--   B. Stock Issue Return
--   C. Stock Transfer (Warehouse → Warehouse)
--   D. Stock Adjustment (Plus / Minus)
--   E. Damage / Loss
--
-- This script is IDEMPOTENT:
--   - Already existing tables are skipped
--   - Already existing rows are skipped
--   - Safe to re-run without creating duplicates
--
-- StockLedger (created in Part 2) is the central movement log.
-- All Part 4 transactions write to StockLedger.
-- Current stock = SUM(InQty) - SUM(OutQty) per WarehouseID + ItemID.
--
-- Target database : OBMSUATNEW (or live equivalent)
-- Part            : 4 – Stock Operations
-- Last updated    : 2026
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Part 4 Stock Operations Migration - Starting...';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : StockIssueDoc
-- The header record for each stock issue transaction.
-- Replaces the old StockIssues (branch-based, no warehouse) for
-- the new ledger-integrated flow. Old table is NOT dropped.
-- IssueToType: Employee | Guard | Branch | Other
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockIssueDoc')
BEGIN
    CREATE TABLE StockIssueDoc (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        IssueNo          NVARCHAR(20)    NOT NULL,
        IssueDate        DATE            NOT NULL,
        WarehouseID      INT             NOT NULL,
        IssueToType      NVARCHAR(20)    NOT NULL,
            -- Values: Employee | Guard | Branch | Other
        IssueToID        INT             NULL,   -- FK to Employee/Branch etc (nullable for Other)
        IssueToName      NVARCHAR(200)   NULL,   -- Free-text name (used for Other / when no FK)
        Remarks          NVARCHAR(500)   NULL,
        IsActive         BIT             NOT NULL DEFAULT 1,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_SID_Warehouse FOREIGN KEY (WarehouseID) REFERENCES WarehouseMaster(ID)
    );
    CREATE UNIQUE INDEX UX_StockIssueDoc_IssueNo
        ON StockIssueDoc(IssueNo) WHERE IsActive = 1;
    CREATE INDEX IX_StockIssueDoc_WarehouseID ON StockIssueDoc(WarehouseID);
    CREATE INDEX IX_StockIssueDoc_IssueDate   ON StockIssueDoc(IssueDate);
    PRINT 'StockIssueDoc table created.';
END
ELSE
    PRINT 'StockIssueDoc already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : StockIssueDocDetail
-- Line items for each issue document.
-- Each line → one StockLedger ISSUE row.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockIssueDocDetail')
BEGIN
    CREATE TABLE StockIssueDocDetail (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        IssueDocID       INT             NOT NULL,
        ItemID           INT             NOT NULL,
        UOMID            INT             NULL,
        IssueQty         DECIMAL(18,2)   NOT NULL,
        UnitCost         DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Remarks          NVARCHAR(500)   NULL,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_SIDDetail_Doc   FOREIGN KEY (IssueDocID) REFERENCES StockIssueDoc(ID),
        CONSTRAINT FK_SIDDetail_Item  FOREIGN KEY (ItemID)     REFERENCES ItemMaster(ID),
        CONSTRAINT FK_SIDDetail_UOM   FOREIGN KEY (UOMID)      REFERENCES UOMMaster(ID)
    );
    CREATE INDEX IX_SIDDetail_IssueDocID ON StockIssueDocDetail(IssueDocID);
    CREATE INDEX IX_SIDDetail_ItemID     ON StockIssueDocDetail(ItemID);
    PRINT 'StockIssueDocDetail table created.';
END
ELSE
    PRINT 'StockIssueDocDetail already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : StockIssueReturn
-- Tracks returns of previously issued stock.
-- Linked back to the original StockIssueDoc header.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockIssueReturn')
BEGIN
    CREATE TABLE StockIssueReturn (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        ReturnNo         NVARCHAR(20)    NOT NULL,
        ReturnDate       DATE            NOT NULL,
        OriginalIssueDocID INT           NOT NULL,   -- FK back to StockIssueDoc
        WarehouseID      INT             NOT NULL,
        Remarks          NVARCHAR(500)   NULL,
        IsActive         BIT             NOT NULL DEFAULT 1,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_SIR_IssueDoc  FOREIGN KEY (OriginalIssueDocID) REFERENCES StockIssueDoc(ID),
        CONSTRAINT FK_SIR_Warehouse FOREIGN KEY (WarehouseID)         REFERENCES WarehouseMaster(ID)
    );
    CREATE UNIQUE INDEX UX_StockIssueReturn_ReturnNo
        ON StockIssueReturn(ReturnNo) WHERE IsActive = 1;
    CREATE INDEX IX_SIR_OriginalIssueDocID ON StockIssueReturn(OriginalIssueDocID);
    PRINT 'StockIssueReturn table created.';
END
ELSE
    PRINT 'StockIssueReturn already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 4 : StockIssueReturnDetail
-- Line items for each return.
-- Each line links back to the original issue detail line
-- to allow validation of return qty vs issued qty.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockIssueReturnDetail')
BEGIN
    CREATE TABLE StockIssueReturnDetail (
        ID                   INT             PRIMARY KEY IDENTITY(1,1),
        ReturnID             INT             NOT NULL,
        OriginalIssueDetailID INT            NOT NULL,   -- FK → StockIssueDocDetail
        ItemID               INT             NOT NULL,
        UOMID                INT             NULL,
        ReturnQty            DECIMAL(18,2)   NOT NULL,
        UnitCost             DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Remarks              NVARCHAR(500)   NULL,
        LASTUPDATE           DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy        NVARCHAR(50)    NULL,

        CONSTRAINT FK_SIRDetail_Return       FOREIGN KEY (ReturnID)              REFERENCES StockIssueReturn(ID),
        CONSTRAINT FK_SIRDetail_IssueDetail  FOREIGN KEY (OriginalIssueDetailID) REFERENCES StockIssueDocDetail(ID),
        CONSTRAINT FK_SIRDetail_Item         FOREIGN KEY (ItemID)                REFERENCES ItemMaster(ID),
        CONSTRAINT FK_SIRDetail_UOM          FOREIGN KEY (UOMID)                 REFERENCES UOMMaster(ID)
    );
    CREATE INDEX IX_SIRDetail_ReturnID            ON StockIssueReturnDetail(ReturnID);
    CREATE INDEX IX_SIRDetail_OriginalIssueDetailID ON StockIssueReturnDetail(OriginalIssueDetailID);
    PRINT 'StockIssueReturnDetail table created.';
END
ELSE
    PRINT 'StockIssueReturnDetail already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 5 : StockTransfer
-- Warehouse-to-warehouse transfer header.
-- Creates TRANSFER_OUT (source) + TRANSFER_IN (dest) ledger rows.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTransfer')
BEGIN
    CREATE TABLE StockTransfer (
        ID                   INT             PRIMARY KEY IDENTITY(1,1),
        TransferNo           NVARCHAR(20)    NOT NULL,
        TransferDate         DATE            NOT NULL,
        SourceWarehouseID    INT             NOT NULL,
        DestWarehouseID      INT             NOT NULL,
        Remarks              NVARCHAR(500)   NULL,
        IsActive             BIT             NOT NULL DEFAULT 1,
        LASTUPDATE           DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy        NVARCHAR(50)    NULL,

        CONSTRAINT FK_ST_SourceWarehouse FOREIGN KEY (SourceWarehouseID) REFERENCES WarehouseMaster(ID),
        CONSTRAINT FK_ST_DestWarehouse   FOREIGN KEY (DestWarehouseID)   REFERENCES WarehouseMaster(ID)
    );
    CREATE UNIQUE INDEX UX_StockTransfer_TransferNo
        ON StockTransfer(TransferNo) WHERE IsActive = 1;
    CREATE INDEX IX_ST_SourceWarehouseID ON StockTransfer(SourceWarehouseID);
    CREATE INDEX IX_ST_DestWarehouseID   ON StockTransfer(DestWarehouseID);
    PRINT 'StockTransfer table created.';
END
ELSE
    PRINT 'StockTransfer already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 6 : StockTransferDetail
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockTransferDetail')
BEGIN
    CREATE TABLE StockTransferDetail (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        TransferID       INT             NOT NULL,
        ItemID           INT             NOT NULL,
        UOMID            INT             NULL,
        TransferQty      DECIMAL(18,2)   NOT NULL,
        UnitCost         DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Remarks          NVARCHAR(500)   NULL,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_STDetail_Transfer FOREIGN KEY (TransferID) REFERENCES StockTransfer(ID),
        CONSTRAINT FK_STDetail_Item     FOREIGN KEY (ItemID)     REFERENCES ItemMaster(ID),
        CONSTRAINT FK_STDetail_UOM      FOREIGN KEY (UOMID)      REFERENCES UOMMaster(ID)
    );
    CREATE INDEX IX_STDetail_TransferID ON StockTransferDetail(TransferID);
    CREATE INDEX IX_STDetail_ItemID     ON StockTransferDetail(ItemID);
    PRINT 'StockTransferDetail table created.';
END
ELSE
    PRINT 'StockTransferDetail already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 7 : StockAdjustment
-- Physical count discrepancy adjustment.
-- AdjustmentType: PLUS | MINUS
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockAdjustment')
BEGIN
    CREATE TABLE StockAdjustment (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        AdjustmentNo     NVARCHAR(20)    NOT NULL,
        AdjustmentDate   DATE            NOT NULL,
        WarehouseID      INT             NOT NULL,
        ItemID           INT             NOT NULL,
        AdjustmentType   NVARCHAR(10)    NOT NULL,   -- PLUS | MINUS
        AdjustmentQty    DECIMAL(18,2)   NOT NULL,
        UnitCost         DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Reason           NVARCHAR(500)   NOT NULL,
        Remarks          NVARCHAR(500)   NULL,
        IsActive         BIT             NOT NULL DEFAULT 1,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_SA_Warehouse FOREIGN KEY (WarehouseID) REFERENCES WarehouseMaster(ID),
        CONSTRAINT FK_SA_Item      FOREIGN KEY (ItemID)      REFERENCES ItemMaster(ID)
    );
    CREATE UNIQUE INDEX UX_StockAdjustment_AdjustmentNo
        ON StockAdjustment(AdjustmentNo) WHERE IsActive = 1;
    CREATE INDEX IX_SA_WarehouseID ON StockAdjustment(WarehouseID);
    CREATE INDEX IX_SA_ItemID      ON StockAdjustment(ItemID);
    PRINT 'StockAdjustment table created.';
END
ELSE
    PRINT 'StockAdjustment already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 8 : StockDamageLoss
-- Records damaged or lost items that reduce available stock.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockDamageLoss')
BEGIN
    CREATE TABLE StockDamageLoss (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        RecordNo         NVARCHAR(20)    NOT NULL,
        RecordDate       DATE            NOT NULL,
        WarehouseID      INT             NOT NULL,
        ItemID           INT             NOT NULL,
        LossType         NVARCHAR(20)    NOT NULL DEFAULT 'Damage',  -- Damage | Loss
        Quantity         DECIMAL(18,2)   NOT NULL,
        UnitCost         DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Reason           NVARCHAR(500)   NOT NULL,
        Remarks          NVARCHAR(500)   NULL,
        IsActive         BIT             NOT NULL DEFAULT 1,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_SDL_Warehouse FOREIGN KEY (WarehouseID) REFERENCES WarehouseMaster(ID),
        CONSTRAINT FK_SDL_Item      FOREIGN KEY (ItemID)      REFERENCES ItemMaster(ID)
    );
    CREATE UNIQUE INDEX UX_StockDamageLoss_RecordNo
        ON StockDamageLoss(RecordNo) WHERE IsActive = 1;
    CREATE INDEX IX_SDL_WarehouseID ON StockDamageLoss(WarehouseID);
    CREATE INDEX IX_SDL_ItemID      ON StockDamageLoss(ItemID);
    PRINT 'StockDamageLoss table created.';
END
ELSE
    PRINT 'StockDamageLoss already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 9 : SCREENLIST ENTRIES
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Issue')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Issue', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Stock Issue added.';
END
ELSE PRINT 'ScreenList: Stock Issue already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Issue Return')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Issue Return', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Stock Issue Return added.';
END
ELSE PRINT 'ScreenList: Stock Issue Return already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Transfer')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Transfer', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Stock Transfer added.';
END
ELSE PRINT 'ScreenList: Stock Transfer already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Adjustment')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Adjustment', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Stock Adjustment added.';
END
ELSE PRINT 'ScreenList: Stock Adjustment already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Damage / Loss')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Damage / Loss', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Damage / Loss added.';
END
ELSE PRINT 'ScreenList: Damage / Loss already exists. Skipping.';

PRINT '============================================================';
PRINT 'OBMS Part 4 Stock Operations Migration - Completed.';
PRINT '============================================================';
