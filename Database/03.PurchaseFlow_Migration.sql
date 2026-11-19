-- ============================================================
-- OBMS Inventory Part 3 Migration
-- Purchase Flow: PR → PO → GRN → Purchase Return
--
-- This script is IDEMPOTENT:
--   - Already existing tables are skipped
--   - Already existing rows are skipped
--   - Safe to re-run without creating duplicates
--
-- Target database : OBMSUATNEW (or live equivalent)
-- Part            : 3 - Purchase Flow
-- Last updated    : 2026
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Part 3 Purchase Flow Migration - Starting...';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : PurchaseRequisition
-- Status lifecycle: Draft → Submitted → Approved / Rejected
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PurchaseRequisition')
BEGIN
    CREATE TABLE PurchaseRequisition (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        PRNumber         NVARCHAR(20)    NOT NULL,
        PRDate           DATE            NOT NULL,
        RequiredByDate   DATE            NOT NULL,
        Status           NVARCHAR(20)    NOT NULL DEFAULT 'Draft',
            -- Values: Draft | Submitted | Approved | Rejected
        Remarks          NVARCHAR(500)   NULL,
        ApprovedBy       NVARCHAR(50)    NULL,
        ApprovedDate     DATETIME        NULL,
        ApprovalRemarks  NVARCHAR(500)   NULL,
        IsActive         BIT             NOT NULL DEFAULT 1,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL
    );
    CREATE UNIQUE INDEX UX_PurchaseRequisition_PRNumber
        ON PurchaseRequisition(PRNumber) WHERE IsActive = 1;
    CREATE INDEX IX_PurchaseRequisition_Status ON PurchaseRequisition(Status);
    PRINT 'PurchaseRequisition table created.';
END
ELSE
    PRINT 'PurchaseRequisition already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : PurchaseRequisitionDetail
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PurchaseRequisitionDetail')
BEGIN
    CREATE TABLE PurchaseRequisitionDetail (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        PRID             INT             NOT NULL,
        ItemID           INT             NOT NULL,
        RequiredQty      DECIMAL(18,2)   NOT NULL,
        UOMID            INT             NULL,
        Remarks          NVARCHAR(500)   NULL,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_PRDetail_PR     FOREIGN KEY (PRID)    REFERENCES PurchaseRequisition(ID),
        CONSTRAINT FK_PRDetail_Item   FOREIGN KEY (ItemID)  REFERENCES ItemMaster(ID),
        CONSTRAINT FK_PRDetail_UOM    FOREIGN KEY (UOMID)   REFERENCES UOMMaster(ID)
    );
    CREATE INDEX IX_PRDetail_PRID   ON PurchaseRequisitionDetail(PRID);
    CREATE INDEX IX_PRDetail_ItemID ON PurchaseRequisitionDetail(ItemID);
    PRINT 'PurchaseRequisitionDetail table created.';
END
ELSE
    PRINT 'PurchaseRequisitionDetail already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : PurchaseOrder
-- Status lifecycle: Draft → Approved → Partially Received → Fully Received / Cancelled
-- PRID is nullable — PO can be created directly without a PR
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PurchaseOrder')
BEGIN
    CREATE TABLE PurchaseOrder (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        PONumber         NVARCHAR(20)    NOT NULL,
        PRID             INT             NULL,       -- nullable: PO can be independent
        SupplierID       INT             NOT NULL,
        WarehouseID      INT             NOT NULL,
        PODate           DATE            NOT NULL,
        ExpectedDate     DATE            NOT NULL,
        Status           NVARCHAR(30)    NOT NULL DEFAULT 'Draft',
            -- Values: Draft | Approved | Partially Received | Fully Received | Cancelled
        TotalAmount      DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Remarks          NVARCHAR(500)   NULL,
        ApprovedBy       NVARCHAR(50)    NULL,
        ApprovedDate     DATETIME        NULL,
        IsActive         BIT             NOT NULL DEFAULT 1,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_PO_PR           FOREIGN KEY (PRID)        REFERENCES PurchaseRequisition(ID),
        CONSTRAINT FK_PO_Supplier     FOREIGN KEY (SupplierID)  REFERENCES Suppliers(Id),
        CONSTRAINT FK_PO_Warehouse    FOREIGN KEY (WarehouseID) REFERENCES WarehouseMaster(ID)
    );
    CREATE UNIQUE INDEX UX_PurchaseOrder_PONumber
        ON PurchaseOrder(PONumber) WHERE IsActive = 1;
    CREATE INDEX IX_PurchaseOrder_Status     ON PurchaseOrder(Status);
    CREATE INDEX IX_PurchaseOrder_SupplierID ON PurchaseOrder(SupplierID);
    CREATE INDEX IX_PurchaseOrder_PRID       ON PurchaseOrder(PRID);
    PRINT 'PurchaseOrder table created.';
END
ELSE
    PRINT 'PurchaseOrder already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 4 : PurchaseOrderDetail
-- ReceivedQty is the cumulative total received across all GRNs
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PurchaseOrderDetail')
BEGIN
    CREATE TABLE PurchaseOrderDetail (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        POID             INT             NOT NULL,
        ItemID           INT             NOT NULL,
        UOMID            INT             NULL,
        OrderedQty       DECIMAL(18,2)   NOT NULL,
        ReceivedQty      DECIMAL(18,2)   NOT NULL DEFAULT 0,
        UnitPrice        DECIMAL(18,4)   NOT NULL DEFAULT 0,
        TotalPrice       DECIMAL(18,4)   NOT NULL DEFAULT 0,
        Remarks          NVARCHAR(500)   NULL,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_PODetail_PO   FOREIGN KEY (POID)   REFERENCES PurchaseOrder(ID),
        CONSTRAINT FK_PODetail_Item FOREIGN KEY (ItemID) REFERENCES ItemMaster(ID),
        CONSTRAINT FK_PODetail_UOM  FOREIGN KEY (UOMID)  REFERENCES UOMMaster(ID)
    );
    CREATE INDEX IX_PODetail_POID   ON PurchaseOrderDetail(POID);
    CREATE INDEX IX_PODetail_ItemID ON PurchaseOrderDetail(ItemID);
    PRINT 'PurchaseOrderDetail table created.';
END
ELSE
    PRINT 'PurchaseOrderDetail already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 5 : GoodsReceiptNote
-- Each GRN is against one PO. Multiple GRNs allowed per PO.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GoodsReceiptNote')
BEGIN
    CREATE TABLE GoodsReceiptNote (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        GRNNumber        NVARCHAR(20)    NOT NULL,
        POID             INT             NOT NULL,
        WarehouseID      INT             NOT NULL,
        GRNDate          DATE            NOT NULL,
        DeliveryNoteRef  NVARCHAR(100)   NULL,
        Remarks          NVARCHAR(500)   NULL,
        IsActive         BIT             NOT NULL DEFAULT 1,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_GRN_PO        FOREIGN KEY (POID)        REFERENCES PurchaseOrder(ID),
        CONSTRAINT FK_GRN_Warehouse FOREIGN KEY (WarehouseID) REFERENCES WarehouseMaster(ID)
    );
    CREATE UNIQUE INDEX UX_GoodsReceiptNote_GRNNumber
        ON GoodsReceiptNote(GRNNumber) WHERE IsActive = 1;
    CREATE INDEX IX_GRN_POID        ON GoodsReceiptNote(POID);
    CREATE INDEX IX_GRN_WarehouseID ON GoodsReceiptNote(WarehouseID);
    PRINT 'GoodsReceiptNote table created.';
END
ELSE
    PRINT 'GoodsReceiptNote already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 6 : GoodsReceiptNoteDetail
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'GoodsReceiptNoteDetail')
BEGIN
    CREATE TABLE GoodsReceiptNoteDetail (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        GRNID            INT             NOT NULL,
        PODetailID       INT             NOT NULL,
        ItemID           INT             NOT NULL,
        UOMID            INT             NULL,
        ReceivedQty      DECIMAL(18,2)   NOT NULL,
        UnitCost         DECIMAL(18,4)   NOT NULL DEFAULT 0,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_GRNDetail_GRN      FOREIGN KEY (GRNID)      REFERENCES GoodsReceiptNote(ID),
        CONSTRAINT FK_GRNDetail_PODetail FOREIGN KEY (PODetailID) REFERENCES PurchaseOrderDetail(ID),
        CONSTRAINT FK_GRNDetail_Item     FOREIGN KEY (ItemID)     REFERENCES ItemMaster(ID),
        CONSTRAINT FK_GRNDetail_UOM      FOREIGN KEY (UOMID)      REFERENCES UOMMaster(ID)
    );
    CREATE INDEX IX_GRNDetail_GRNID     ON GoodsReceiptNoteDetail(GRNID);
    CREATE INDEX IX_GRNDetail_PODetailID ON GoodsReceiptNoteDetail(PODetailID);
    CREATE INDEX IX_GRNDetail_ItemID    ON GoodsReceiptNoteDetail(ItemID);
    PRINT 'GoodsReceiptNoteDetail table created.';
END
ELSE
    PRINT 'GoodsReceiptNoteDetail already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 7 : PurchaseReturn
-- Each return is against one GRN.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PurchaseReturn')
BEGIN
    CREATE TABLE PurchaseReturn (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        ReturnNumber     NVARCHAR(20)    NOT NULL,
        GRNID            INT             NOT NULL,
        WarehouseID      INT             NOT NULL,
        ReturnDate       DATE            NOT NULL,
        Reason           NVARCHAR(500)   NOT NULL,
        IsActive         BIT             NOT NULL DEFAULT 1,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_Return_GRN       FOREIGN KEY (GRNID)       REFERENCES GoodsReceiptNote(ID),
        CONSTRAINT FK_Return_Warehouse FOREIGN KEY (WarehouseID) REFERENCES WarehouseMaster(ID)
    );
    CREATE UNIQUE INDEX UX_PurchaseReturn_ReturnNumber
        ON PurchaseReturn(ReturnNumber) WHERE IsActive = 1;
    CREATE INDEX IX_PurchaseReturn_GRNID ON PurchaseReturn(GRNID);
    PRINT 'PurchaseReturn table created.';
END
ELSE
    PRINT 'PurchaseReturn already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 8 : PurchaseReturnDetail
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'PurchaseReturnDetail')
BEGIN
    CREATE TABLE PurchaseReturnDetail (
        ID               INT             PRIMARY KEY IDENTITY(1,1),
        ReturnID         INT             NOT NULL,
        GRNDetailID      INT             NOT NULL,
        ItemID           INT             NOT NULL,
        UOMID            INT             NULL,
        ReturnQty        DECIMAL(18,2)   NOT NULL,
        UnitCost         DECIMAL(18,4)   NOT NULL DEFAULT 0,
        LASTUPDATE       DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)    NULL,

        CONSTRAINT FK_ReturnDetail_Return    FOREIGN KEY (ReturnID)    REFERENCES PurchaseReturn(ID),
        CONSTRAINT FK_ReturnDetail_GRNDetail FOREIGN KEY (GRNDetailID) REFERENCES GoodsReceiptNoteDetail(ID),
        CONSTRAINT FK_ReturnDetail_Item      FOREIGN KEY (ItemID)      REFERENCES ItemMaster(ID),
        CONSTRAINT FK_ReturnDetail_UOM       FOREIGN KEY (UOMID)       REFERENCES UOMMaster(ID)
    );
    CREATE INDEX IX_ReturnDetail_ReturnID   ON PurchaseReturnDetail(ReturnID);
    CREATE INDEX IX_ReturnDetail_GRNDetailID ON PurchaseReturnDetail(GRNDetailID);
    PRINT 'PurchaseReturnDetail table created.';
END
ELSE
    PRINT 'PurchaseReturnDetail already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 9 : SCREENLIST ENTRIES
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Purchase Requisition')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Purchase Requisition', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Purchase Requisition added.';
END
ELSE PRINT 'ScreenList: Purchase Requisition already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Purchase Order')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Purchase Order', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Purchase Order added.';
END
ELSE PRINT 'ScreenList: Purchase Order already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Goods Receipt')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Goods Receipt', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Goods Receipt added.';
END
ELSE PRINT 'ScreenList: Goods Receipt already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Purchase Return')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Purchase Return', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Purchase Return added.';
END
ELSE PRINT 'ScreenList: Purchase Return already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
PRINT '============================================================';
PRINT 'OBMS Part 3 Purchase Flow Migration - Completed Successfully.';
PRINT '============================================================';
