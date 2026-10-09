-- ============================================================
-- OBMS Inventory Part 7 Migration
-- Approval Workflow + Notification Framework
--
-- IDEMPOTENT — safe to re-run.
-- Uses GO batch separators + conditional DDL.
--
-- Target database : OBMSUATNEW (or live equivalent)
-- Part            : 7 – Approval Workflow + Notifications
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Part 7 Approval Workflow + Notification Migration';
PRINT 'Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : ApprovalConfiguration
-- Controls which document types require approval.
-- One row per document type.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'ApprovalConfiguration'
)
BEGIN
    CREATE TABLE ApprovalConfiguration (
        ID              INT IDENTITY(1,1) PRIMARY KEY,
        DocumentType    NVARCHAR(50)  NOT NULL,   -- PURCHASE_REQUISITION, PURCHASE_ORDER, etc.
        DisplayName     NVARCHAR(100) NOT NULL,
        ApprovalRequired BIT NOT NULL DEFAULT 1,
        ApproverRole    NVARCHAR(100) NULL,        -- informational / future use
        Remarks         NVARCHAR(500) NULL,
        IsActive        BIT NOT NULL DEFAULT 1,
        LASTUPDATE      DATETIME NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy   NVARCHAR(100) NULL
    );
    PRINT 'ApprovalConfiguration: table created.';
END
ELSE PRINT 'ApprovalConfiguration: already exists.';
GO

-- Seed default configuration rows (idempotent)
IF NOT EXISTS (SELECT 1 FROM ApprovalConfiguration WHERE DocumentType = 'PURCHASE_REQUISITION')
BEGIN
    INSERT INTO ApprovalConfiguration (DocumentType, DisplayName, ApprovalRequired, ApproverRole, Remarks, LastUpdatedBy)
    VALUES ('PURCHASE_REQUISITION', 'Purchase Requisition', 1, 'Inventory Manager', 'PR must be approved before creating a PO.', 'system');
    PRINT 'ApprovalConfiguration: PURCHASE_REQUISITION seeded.';
END
GO

IF NOT EXISTS (SELECT 1 FROM ApprovalConfiguration WHERE DocumentType = 'PURCHASE_ORDER')
BEGIN
    INSERT INTO ApprovalConfiguration (DocumentType, DisplayName, ApprovalRequired, ApproverRole, Remarks, LastUpdatedBy)
    VALUES ('PURCHASE_ORDER', 'Purchase Order', 1, 'Inventory Manager', 'PO must be approved before GRN can be created.', 'system');
    PRINT 'ApprovalConfiguration: PURCHASE_ORDER seeded.';
END
GO

IF NOT EXISTS (SELECT 1 FROM ApprovalConfiguration WHERE DocumentType = 'STOCK_ADJUSTMENT')
BEGIN
    INSERT INTO ApprovalConfiguration (DocumentType, DisplayName, ApprovalRequired, ApproverRole, Remarks, LastUpdatedBy)
    VALUES ('STOCK_ADJUSTMENT', 'Stock Adjustment', 0, 'Inventory Manager', 'Stock Adjustment - approval not required by default.', 'system');
    PRINT 'ApprovalConfiguration: STOCK_ADJUSTMENT seeded.';
END
GO

IF NOT EXISTS (SELECT 1 FROM ApprovalConfiguration WHERE DocumentType = 'STOCK_TRANSFER')
BEGIN
    INSERT INTO ApprovalConfiguration (DocumentType, DisplayName, ApprovalRequired, ApproverRole, Remarks, LastUpdatedBy)
    VALUES ('STOCK_TRANSFER', 'Stock Transfer', 0, 'Inventory Manager', 'Stock Transfer - approval not required by default.', 'system');
    PRINT 'ApprovalConfiguration: STOCK_TRANSFER seeded.';
END
GO

IF NOT EXISTS (SELECT 1 FROM ApprovalConfiguration WHERE DocumentType = 'DAMAGE_LOSS')
BEGIN
    INSERT INTO ApprovalConfiguration (DocumentType, DisplayName, ApprovalRequired, ApproverRole, Remarks, LastUpdatedBy)
    VALUES ('DAMAGE_LOSS', 'Damage / Loss', 0, 'Inventory Manager', 'Damage/Loss - approval not required by default.', 'system');
    PRINT 'ApprovalConfiguration: DAMAGE_LOSS seeded.';
END
GO

IF NOT EXISTS (SELECT 1 FROM ApprovalConfiguration WHERE DocumentType = 'ASSET_DISPOSAL')
BEGIN
    INSERT INTO ApprovalConfiguration (DocumentType, DisplayName, ApprovalRequired, ApproverRole, Remarks, LastUpdatedBy)
    VALUES ('ASSET_DISPOSAL', 'Asset Disposal', 0, 'Inventory Manager', 'Asset Disposal - approval not required by default.', 'system');
    PRINT 'ApprovalConfiguration: ASSET_DISPOSAL seeded.';
END
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : ApprovalHistory
-- Immutable audit trail of every submit / approve / reject action.
-- One row per action. Never deleted, never updated.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'ApprovalHistory'
)
BEGIN
    CREATE TABLE ApprovalHistory (
        ID              INT IDENTITY(1,1) PRIMARY KEY,
        DocumentType    NVARCHAR(50)  NOT NULL,   -- PURCHASE_REQUISITION | PURCHASE_ORDER | ...
        DocumentID      INT           NOT NULL,   -- FK to the source document
        DocumentNumber  NVARCHAR(50)  NOT NULL,   -- Human-readable: PR0001, PO0001, etc.
        Action          NVARCHAR(30)  NOT NULL,   -- Submitted | Approved | Rejected | Cancelled
        ActionBy        NVARCHAR(100) NOT NULL,
        ActionDate      DATETIME      NOT NULL DEFAULT GETDATE(),
        Comments        NVARCHAR(1000) NULL,      -- Approval remarks or reject reason
        PreviousStatus  NVARCHAR(50)  NULL,
        NewStatus       NVARCHAR(50)  NULL
    );
    PRINT 'ApprovalHistory: table created.';
END
ELSE PRINT 'ApprovalHistory: already exists.';
GO

-- Index for fast lookup by document
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApprovalHistory_Doc'
      AND object_id = OBJECT_ID('ApprovalHistory')
)
BEGIN
    CREATE INDEX IX_ApprovalHistory_Doc
        ON ApprovalHistory (DocumentType, DocumentID);
    PRINT 'ApprovalHistory: IX_ApprovalHistory_Doc index created.';
END
ELSE PRINT 'ApprovalHistory: IX_ApprovalHistory_Doc already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : AppNotification
-- Stores notifications for individual users.
-- Supports: read/unread, soft-delete, reference to source doc.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'AppNotification'
)
BEGIN
    CREATE TABLE AppNotification (
        ID              INT IDENTITY(1,1) PRIMARY KEY,
        NotificationType NVARCHAR(50) NOT NULL,   -- PENDING_APPROVAL | APPROVED | REJECTED | LOW_STOCK | OUT_OF_STOCK
        Title           NVARCHAR(200) NOT NULL,
        Message         NVARCHAR(1000) NOT NULL,
        RecipientUser   NVARCHAR(100) NOT NULL,   -- username of the target user
        ReferenceType   NVARCHAR(50)  NULL,       -- PURCHASE_REQUISITION | ITEM | etc.
        ReferenceID     INT           NULL,
        ReferenceNumber NVARCHAR(50)  NULL,       -- PR0001 / item code / etc.
        IsRead          BIT NOT NULL DEFAULT 0,
        ReadDate        DATETIME NULL,
        IsActive        BIT NOT NULL DEFAULT 1,   -- soft-delete
        CreatedDate     DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy       NVARCHAR(100) NULL
    );
    PRINT 'AppNotification: table created.';
END
ELSE PRINT 'AppNotification: already exists.';
GO

-- Index for fast unread-count queries per user
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AppNotification_User_Read'
      AND object_id = OBJECT_ID('AppNotification')
)
BEGIN
    CREATE INDEX IX_AppNotification_User_Read
        ON AppNotification (RecipientUser, IsRead, IsActive);
    PRINT 'AppNotification: IX_AppNotification_User_Read index created.';
END
ELSE PRINT 'AppNotification: IX_AppNotification_User_Read already exists.';
GO

-- Index for deduplication check (LOW_STOCK per item)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AppNotification_Type_Ref'
      AND object_id = OBJECT_ID('AppNotification')
)
BEGIN
    CREATE INDEX IX_AppNotification_Type_Ref
        ON AppNotification (NotificationType, ReferenceType, ReferenceID, IsActive);
    PRINT 'AppNotification: IX_AppNotification_Type_Ref index created.';
END
ELSE PRINT 'AppNotification: IX_AppNotification_Type_Ref already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 4 : Add SubmittedBy / SubmittedDate to PurchaseOrder
-- PO currently jumps straight from Draft → Approved.
-- Adding Submitted state to PO (mirrors PR) for consistency.
-- Existing Draft/Approved/Cancelled/PartiallyReceived/FullyReceived
-- behaviour is NOT changed — Submitted is optional extra state.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='PurchaseOrder' AND COLUMN_NAME='SubmittedBy')
BEGIN
    ALTER TABLE PurchaseOrder ADD SubmittedBy NVARCHAR(100) NULL;
    PRINT 'PurchaseOrder: SubmittedBy added.';
END
ELSE PRINT 'PurchaseOrder: SubmittedBy already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='PurchaseOrder' AND COLUMN_NAME='SubmittedDate')
BEGIN
    ALTER TABLE PurchaseOrder ADD SubmittedDate DATETIME NULL;
    PRINT 'PurchaseOrder: SubmittedDate added.';
END
ELSE PRINT 'PurchaseOrder: SubmittedDate already exists.';
GO

-- Add RejectReason to PurchaseOrder (mirrors ApprovalRemarks on PR)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='PurchaseOrder' AND COLUMN_NAME='RejectReason')
BEGIN
    ALTER TABLE PurchaseOrder ADD RejectReason NVARCHAR(1000) NULL;
    PRINT 'PurchaseOrder: RejectReason added.';
END
ELSE PRINT 'PurchaseOrder: RejectReason already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 5 : Add SubmittedBy / SubmittedDate to PurchaseRequisition
-- PR already has ApprovedBy/ApprovedDate. Adding submitted tracking.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='PurchaseRequisition' AND COLUMN_NAME='SubmittedBy')
BEGIN
    ALTER TABLE PurchaseRequisition ADD SubmittedBy NVARCHAR(100) NULL;
    PRINT 'PurchaseRequisition: SubmittedBy added.';
END
ELSE PRINT 'PurchaseRequisition: SubmittedBy already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='PurchaseRequisition' AND COLUMN_NAME='SubmittedDate')
BEGIN
    ALTER TABLE PurchaseRequisition ADD SubmittedDate DATETIME NULL;
    PRINT 'PurchaseRequisition: SubmittedDate added.';
END
ELSE PRINT 'PurchaseRequisition: SubmittedDate already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 6 : Seed OBMSPermissions rows for new screens
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM OBMSPermissions WHERE ScreenName = 'Pending Approvals')
BEGIN
    INSERT INTO OBMSPermissions (Name, ScreenName, [Create], [Read], [Update], [Delete], LastUpdatedBy)
    VALUES ('Pending Approvals', 'Pending Approvals', 0, 1, 1, 0, 'system');
    PRINT 'OBMSPermissions: Pending Approvals row seeded.';
END
ELSE PRINT 'OBMSPermissions: Pending Approvals already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM OBMSPermissions WHERE ScreenName = 'Approval History')
BEGIN
    INSERT INTO OBMSPermissions (Name, ScreenName, [Create], [Read], [Update], [Delete], LastUpdatedBy)
    VALUES ('Approval History', 'Approval History', 0, 1, 0, 0, 'system');
    PRINT 'OBMSPermissions: Approval History row seeded.';
END
ELSE PRINT 'OBMSPermissions: Approval History already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM OBMSPermissions WHERE ScreenName = 'Notifications')
BEGIN
    INSERT INTO OBMSPermissions (Name, ScreenName, [Create], [Read], [Update], [Delete], LastUpdatedBy)
    VALUES ('Notifications', 'Notifications', 0, 1, 1, 0, 'system');
    PRINT 'OBMSPermissions: Notifications row seeded.';
END
ELSE PRINT 'OBMSPermissions: Notifications already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 7 : Useful views for pending approvals dashboard
-- ─────────────────────────────────────────────────────────────

-- VW_PendingApprovals : union of all document types pending approval
IF EXISTS (SELECT 1 FROM sys.views WHERE name = 'VW_PendingApprovals')
    DROP VIEW VW_PendingApprovals;
GO

CREATE VIEW VW_PendingApprovals AS
-- Purchase Requisitions pending approval (Status = 'Submitted')
-- FIX: use SubmittedBy (the JWT user who submitted) as RequestedBy,
--      not LastUpdatedBy (which may be a different user who edited the draft).
SELECT
    'PURCHASE_REQUISITION' AS DocumentType,
    'Purchase Requisition'  AS DocumentTypeDisplay,
    pr.ID                   AS DocumentID,
    pr.PRNumber             AS DocumentNumber,
    CONVERT(DATE, pr.PRDate) AS DocumentDate,
    ISNULL(pr.SubmittedBy, pr.LastUpdatedBy) AS RequestedBy,
    pr.SubmittedDate        AS SubmittedDate,
    pr.Status               AS CurrentStatus,
    NULL                    AS WarehouseName,
    NULL                    AS BranchName,
    NULL                    AS TotalAmount
FROM PurchaseRequisition pr
WHERE pr.Status = 'Submitted'
  AND pr.IsActive = 1

UNION ALL

-- Purchase Orders pending approval (Status = 'Submitted')
-- FIX: use SubmittedBy (the JWT user who submitted) as RequestedBy,
--      not LastUpdatedBy (which may be a different user who edited the draft).
SELECT
    'PURCHASE_ORDER'        AS DocumentType,
    'Purchase Order'        AS DocumentTypeDisplay,
    po.ID                   AS DocumentID,
    po.PONumber             AS DocumentNumber,
    CONVERT(DATE, po.PODate) AS DocumentDate,
    ISNULL(po.SubmittedBy, po.LastUpdatedBy) AS RequestedBy,
    po.SubmittedDate        AS SubmittedDate,
    po.Status               AS CurrentStatus,
    wh.Name                 AS WarehouseName,
    NULL                    AS BranchName,
    po.TotalAmount          AS TotalAmount
FROM PurchaseOrder po
LEFT JOIN WarehouseMaster wh ON po.WarehouseID = wh.ID
WHERE po.Status = 'Submitted'
  AND po.IsActive = 1;
GO

PRINT 'VW_PendingApprovals: view created/updated.';
GO

PRINT '============================================================';
PRINT 'OBMS Part 7 Migration completed successfully.';
PRINT '============================================================';
GO
