-- ============================================================
-- OBMS Inventory Part 9 Migration
-- Branch Stock Request Flow:
--   StockRequest       -- branch requests items from a source warehouse
--   StockRequestDetail -- line items per request
--
-- After approval the user creates a normal StockTransfer
-- (existing functionality) which is linked back here via
-- StockRequest.LinkedTransferID.
--
-- STATUS FLOW:
--   Draft  -->  Submitted  -->  Approved | Rejected
--   (After Approved --> user triggers existing Stock Transfer)
--
-- NOTE ON BranchMaster FK:
--   BranchMaster's actual DB primary key is the varchar column "Code",
--   not the integer "ID" column.  The OBMS app treats BranchMaster.ID
--   as a logical EF Core key only -- the database enforces no FK on it
--   from any other table.  Therefore RequestingBranchID is declared as
--   a plain INT with no FK constraint, consistent with every other table
--   in the system that stores a branch reference by integer ID.
--
-- This script is IDEMPOTENT:
--   - Already existing tables are skipped
--   - Already existing rows are skipped
--   - Safe to re-run without creating duplicates
-- ============================================================

-- Required for filtered indexes (WHERE IsActive = 1).
-- SSMS sets this automatically; explicit here so the script works
-- when run via SQLCMD or any tool that defaults QUOTED_IDENTIFIER OFF.
SET QUOTED_IDENTIFIER ON;
GO

PRINT '============================================================';
PRINT 'OBMS Part 9 Branch Stock Request Migration - Starting...';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : StockRequest  (header)
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockRequest')
BEGIN
    CREATE TABLE StockRequest (
        ID                  INT             PRIMARY KEY IDENTITY(1,1),

        -- Auto-generated reference number e.g. SR0001
        RequestNo           NVARCHAR(20)    NOT NULL,

        RequestDate         DATE            NOT NULL DEFAULT CAST(GETDATE() AS DATE),

        -- Branch that is requesting the stock.
        -- No FK constraint: BranchMaster's DB PK is varchar Code, not integer ID.
        -- This mirrors the pattern used by every other table in OBMS.
        RequestingBranchID  INT             NOT NULL,

        -- Warehouse that will fulfil the request (e.g. Chennai Main Warehouse)
        -- FK is safe: WarehouseMaster uses a standard IDENTITY(1,1) PK.
        SourceWarehouseID   INT             NOT NULL,

        -- Authenticated user who created the request (stamped by backend from JWT)
        RequestedBy         NVARCHAR(200)   NOT NULL,

        -- Draft | Submitted | Approved | Rejected
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Draft',

        Remarks             NVARCHAR(500)   NULL,

        SubmittedBy         NVARCHAR(200)   NULL,
        SubmittedDate       DATETIME        NULL,
        ApprovedBy          NVARCHAR(200)   NULL,
        ApprovedDate        DATETIME        NULL,
        ApprovalRemarks     NVARCHAR(500)   NULL,

        -- Populated once a StockTransfer is created to fulfil this request.
        -- Nullable: not yet transferred.
        LinkedTransferID    INT             NULL,

        IsActive            BIT             NOT NULL DEFAULT 1,
        LASTUPDATE          DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy       NVARCHAR(200)   NULL,

        CONSTRAINT FK_SR_Warehouse FOREIGN KEY (SourceWarehouseID) REFERENCES WarehouseMaster(ID),
        CONSTRAINT FK_SR_Transfer  FOREIGN KEY (LinkedTransferID)  REFERENCES StockTransfer(ID)
    );

    PRINT 'StockRequest table created.';
END
ELSE
    PRINT 'StockRequest already exists. Skipping.';

-- ─────────────────────────────────────────────────────────────
-- SECTION 1b : Indexes on StockRequest
-- Created separately so they are applied even if the table
-- already existed from a previous partial run.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_StockRequest_RequestNo' AND object_id = OBJECT_ID('StockRequest'))
BEGIN
    CREATE UNIQUE INDEX UX_StockRequest_RequestNo
        ON StockRequest(RequestNo) WHERE IsActive = 1;
    PRINT 'Index UX_StockRequest_RequestNo created.';
END
ELSE
    PRINT 'Index UX_StockRequest_RequestNo already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SR_RequestingBranchID' AND object_id = OBJECT_ID('StockRequest'))
BEGIN
    CREATE INDEX IX_SR_RequestingBranchID ON StockRequest(RequestingBranchID);
    PRINT 'Index IX_SR_RequestingBranchID created.';
END
ELSE
    PRINT 'Index IX_SR_RequestingBranchID already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SR_SourceWarehouseID' AND object_id = OBJECT_ID('StockRequest'))
BEGIN
    CREATE INDEX IX_SR_SourceWarehouseID ON StockRequest(SourceWarehouseID);
    PRINT 'Index IX_SR_SourceWarehouseID created.';
END
ELSE
    PRINT 'Index IX_SR_SourceWarehouseID already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SR_Status' AND object_id = OBJECT_ID('StockRequest'))
BEGIN
    CREATE INDEX IX_SR_Status ON StockRequest(Status);
    PRINT 'Index IX_SR_Status created.';
END
ELSE
    PRINT 'Index IX_SR_Status already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : StockRequestDetail  (line items)
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockRequestDetail')
BEGIN
    CREATE TABLE StockRequestDetail (
        ID              INT             PRIMARY KEY IDENTITY(1,1),
        RequestID       INT             NOT NULL,
        ItemID          INT             NOT NULL,
        UOMID           INT             NULL,
        RequestedQty    DECIMAL(18,2)   NOT NULL,
        Remarks         NVARCHAR(500)   NULL,
        LASTUPDATE      DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy   NVARCHAR(200)   NULL,

        CONSTRAINT FK_SRD_Request FOREIGN KEY (RequestID) REFERENCES StockRequest(ID),
        CONSTRAINT FK_SRD_Item    FOREIGN KEY (ItemID)    REFERENCES ItemMaster(ID),
        CONSTRAINT FK_SRD_UOM     FOREIGN KEY (UOMID)     REFERENCES UOMMaster(ID)
    );

    CREATE INDEX IX_SRD_RequestID ON StockRequestDetail(RequestID);
    CREATE INDEX IX_SRD_ItemID    ON StockRequestDetail(ItemID);

    PRINT 'StockRequestDetail table created.';
END
ELSE
    PRINT 'StockRequestDetail already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : ScreenList entry
-- Screen name must match exactly the string used in Angular
-- getUserAccessRights() calls.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Branch Stock Request')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Branch Stock Request', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Branch Stock Request added.';
END
ELSE
    PRINT 'ScreenList: Branch Stock Request already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 4 : ApprovalConfiguration entry
-- Registers the document type so the Approval Setup admin
-- screen can manage it.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ApprovalConfiguration WHERE DocumentType = 'BRANCH_STOCK_REQUEST')
BEGIN
    INSERT INTO ApprovalConfiguration (DocumentType, DisplayName, ApprovalRequired, IsActive, LastUpdatedBy, LASTUPDATE)
    VALUES ('BRANCH_STOCK_REQUEST', 'Branch Stock Request', 1, 1, 'MIGRATION', GETDATE());
    PRINT 'ApprovalConfiguration: BRANCH_STOCK_REQUEST added.';
END
ELSE
    PRINT 'ApprovalConfiguration: BRANCH_STOCK_REQUEST already exists. Skipping.';


PRINT '============================================================';
PRINT 'OBMS Part 9 Branch Stock Request Migration - Complete.';
PRINT '============================================================';
