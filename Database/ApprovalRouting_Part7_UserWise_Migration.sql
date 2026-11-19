-- ============================================================
-- OBMS Part 7 – User-Wise Approval Routing Migration
--
-- IDEMPOTENT — safe to re-run on any environment.
-- Uses GO batch separators + conditional DDL.
--
-- Purpose:
--   Adds ApprovalRouting table that maps:
--     DocumentType + RequesterUser  →  ApproverUser
--
--   Fixes the seeded OBMSPermissions rows that had the screen
--   name stored as the user name (Part 7 seed error).
--
--   Adds "Approval Setup" to ScreenList so the administrator
--   can assign access via User Access Rights.
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Part 7 – User-Wise Approval Routing Migration';
PRINT 'Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : ApprovalRouting
-- Maps (DocumentType, RequesterUser) → ApproverUser.
-- One active row per requester per document type.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'ApprovalRouting'
)
BEGIN
    CREATE TABLE ApprovalRouting (
        ID              INT IDENTITY(1,1) PRIMARY KEY,
        DocumentType    NVARCHAR(50)  NOT NULL,   -- PURCHASE_REQUISITION | PURCHASE_ORDER | etc.
        RequesterUser   NVARCHAR(200) NOT NULL,   -- OBMSUsers.Name of the submitter
        ApproverUser    NVARCHAR(200) NOT NULL,   -- OBMSUsers.Name of the designated approver
        IsActive        BIT NOT NULL DEFAULT 1,
        LastUpdatedBy   NVARCHAR(200) NULL,
        LASTUPDATE      DATETIME NOT NULL DEFAULT GETDATE()
    );
    PRINT 'ApprovalRouting: table created.';
END
ELSE
    PRINT 'ApprovalRouting: already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : Indexes on ApprovalRouting
-- ─────────────────────────────────────────────────────────────

-- Fast lookup for SubmitPR / SubmitPO routing
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApprovalRouting_Lookup'
      AND object_id = OBJECT_ID('ApprovalRouting')
)
BEGIN
    CREATE INDEX IX_ApprovalRouting_Lookup
        ON ApprovalRouting (DocumentType, RequesterUser, IsActive);
    PRINT 'ApprovalRouting: IX_ApprovalRouting_Lookup index created.';
END
ELSE
    PRINT 'ApprovalRouting: IX_ApprovalRouting_Lookup already exists.';
GO

-- Index for approver lookups (GetPendingApprovals filter)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_ApprovalRouting_Approver'
      AND object_id = OBJECT_ID('ApprovalRouting')
)
BEGIN
    CREATE INDEX IX_ApprovalRouting_Approver
        ON ApprovalRouting (ApproverUser, IsActive);
    PRINT 'ApprovalRouting: IX_ApprovalRouting_Approver index created.';
END
ELSE
    PRINT 'ApprovalRouting: IX_ApprovalRouting_Approver already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : Unique constraint
-- Prevents duplicate ACTIVE routing for the same
-- DocumentType + RequesterUser combination.
-- (A requester can only have ONE active approver per doc type.)
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_ApprovalRouting_ActiveMapping'
      AND object_id = OBJECT_ID('ApprovalRouting')
)
BEGIN
    -- Filtered unique index: uniqueness only applies to active rows
    CREATE UNIQUE INDEX UQ_ApprovalRouting_ActiveMapping
        ON ApprovalRouting (DocumentType, RequesterUser)
        WHERE IsActive = 1;
    PRINT 'ApprovalRouting: UQ_ApprovalRouting_ActiveMapping unique index created.';
END
ELSE
    PRINT 'ApprovalRouting: UQ_ApprovalRouting_ActiveMapping already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 4 : Fix Part 7 OBMSPermissions seed error
-- The original migration inserted rows with Name = screen name
-- instead of a real username.  Remove those orphan rows so the
-- administrator can assign real permissions via User Access Rights.
-- ─────────────────────────────────────────────────────────────

DELETE FROM OBMSPermissions
WHERE Name IN ('Pending Approvals', 'Approval History', 'Notifications')
  AND ScreenName IN ('Pending Approvals', 'Approval History', 'Notifications')
  AND Name = ScreenName;   -- only delete the exact bad seed rows
PRINT 'OBMSPermissions: bad seed rows removed (Name = ScreenName).';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 5 : Add "Approval Setup" to ScreenList
-- Allows administrator to assign screen access via
-- existing User Access Rights screen.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Approval Setup')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Approval Setup', 'Administration', GETDATE(), 'system');
    PRINT 'ScreenList: Approval Setup added.';
END
ELSE
    PRINT 'ScreenList: Approval Setup already exists.';
GO

-- Ensure the three approval/notification screens are in ScreenList
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Pending Approvals')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Pending Approvals', 'Inventory', GETDATE(), 'system');
    PRINT 'ScreenList: Pending Approvals added.';
END
ELSE
    PRINT 'ScreenList: Pending Approvals already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Approval History')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Approval History', 'Inventory', GETDATE(), 'system');
    PRINT 'ScreenList: Approval History added.';
END
ELSE
    PRINT 'ScreenList: Approval History already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Notifications')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Notifications', 'Inventory', GETDATE(), 'system');
    PRINT 'ScreenList: Notifications added.';
END
ELSE
    PRINT 'ScreenList: Notifications already exists.';
GO

PRINT '============================================================';
PRINT 'OBMS Part 7 User-Wise Routing Migration completed.';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '  1. Run this migration on the target database.';
PRINT '  2. Log in as superadmin.';
PRINT '  3. Go to Administration → User Access Rights.';
PRINT '  4. Assign permissions for each user for:';
PRINT '     - Approval Setup';
PRINT '     - Pending Approvals';
PRINT '     - Approval History';
PRINT '     - Notifications';
PRINT '  5. Go to Administration → Approval Setup.';
PRINT '  6. Configure requester → approver mappings.';
PRINT '============================================================';
GO
