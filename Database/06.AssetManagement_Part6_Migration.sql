-- ============================================================
-- OBMS Inventory Part 6 Migration
-- Asset Management
--
-- IDEMPOTENT — safe to re-run.
-- Uses GO batch separators + EXEC for dynamic DDL to avoid
-- SQL Server parse-time "Invalid column name" errors when
-- adding columns and immediately referencing them in the
-- same script run.
--
-- Target database : OBMSUATNEW (or live equivalent)
-- Part            : 6 – Asset Management
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Part 6 Asset Management Migration - Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 1a : Add new columns to AssetMaster (simple NULLable)
-- These do not reference the new columns so no EXEC needed.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='AssetCode')
BEGIN
    ALTER TABLE AssetMaster ADD AssetCode NVARCHAR(20) NULL;
    PRINT 'AssetMaster: AssetCode added.';
END
ELSE PRINT 'AssetMaster: AssetCode already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='SerialNumber')
BEGIN
    ALTER TABLE AssetMaster ADD SerialNumber NVARCHAR(100) NULL;
    PRINT 'AssetMaster: SerialNumber added.';
END
ELSE PRINT 'AssetMaster: SerialNumber already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='Description')
BEGIN
    ALTER TABLE AssetMaster ADD Description NVARCHAR(500) NULL;
    PRINT 'AssetMaster: Description added.';
END
ELSE PRINT 'AssetMaster: Description already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='ModelNo')
BEGIN
    ALTER TABLE AssetMaster ADD ModelNo NVARCHAR(100) NULL;
    PRINT 'AssetMaster: ModelNo added.';
END
ELSE PRINT 'AssetMaster: ModelNo already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='BrandName')
BEGIN
    ALTER TABLE AssetMaster ADD BrandName NVARCHAR(100) NULL;
    PRINT 'AssetMaster: BrandName added.';
END
ELSE PRINT 'AssetMaster: BrandName already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='SupplierID')
BEGIN
    ALTER TABLE AssetMaster ADD SupplierID INT NULL;
    PRINT 'AssetMaster: SupplierID added.';
END
ELSE PRINT 'AssetMaster: SupplierID already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='WarrantyExpiryDate')
BEGIN
    ALTER TABLE AssetMaster ADD WarrantyExpiryDate DATE NULL;
    PRINT 'AssetMaster: WarrantyExpiryDate added.';
END
ELSE PRINT 'AssetMaster: WarrantyExpiryDate already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='AssetStatus')
BEGIN
    ALTER TABLE AssetMaster ADD AssetStatus NVARCHAR(30) NOT NULL DEFAULT 'Available';
    PRINT 'AssetMaster: AssetStatus added (default = Available).';
END
ELSE PRINT 'AssetMaster: AssetStatus already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='AssetCondition')
BEGIN
    ALTER TABLE AssetMaster ADD AssetCondition NVARCHAR(20) NULL;
    PRINT 'AssetMaster: AssetCondition added.';
END
ELSE PRINT 'AssetMaster: AssetCondition already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='Remarks')
BEGIN
    ALTER TABLE AssetMaster ADD Remarks NVARCHAR(500) NULL;
    PRINT 'AssetMaster: Remarks added.';
END
ELSE PRINT 'AssetMaster: Remarks already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='IsActive')
BEGIN
    ALTER TABLE AssetMaster ADD IsActive BIT NOT NULL DEFAULT 1;
    PRINT 'AssetMaster: IsActive added.';
END
ELSE PRINT 'AssetMaster: IsActive already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='LASTUPDATE')
BEGIN
    ALTER TABLE AssetMaster ADD LASTUPDATE DATETIME NOT NULL DEFAULT GETDATE();
    PRINT 'AssetMaster: LASTUPDATE added.';
END
ELSE PRINT 'AssetMaster: LASTUPDATE already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 1b : Back-fill AssetCode for existing rows
-- New batch (after GO) so AssetCode is now a known column.
-- ─────────────────────────────────────────────────────────────

UPDATE AssetMaster
SET AssetCode = 'AST' + RIGHT('0000' + CAST(ID AS NVARCHAR(10)), 4)
WHERE AssetCode IS NULL;
PRINT 'AssetMaster: AssetCode back-filled for existing rows.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 1c : Unique index on AssetCode
-- New batch so the column is guaranteed to exist.
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AssetMaster_AssetCode'
               AND object_id = OBJECT_ID('AssetMaster'))
BEGIN
    CREATE UNIQUE INDEX UX_AssetMaster_AssetCode
        ON AssetMaster(AssetCode)
        WHERE AssetCode IS NOT NULL;
    PRINT 'AssetMaster: Unique index on AssetCode created.';
END
ELSE PRINT 'AssetMaster: Unique index on AssetCode already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : AssetAllocation
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetAllocation')
BEGIN
    CREATE TABLE AssetAllocation (
        ID               INT           PRIMARY KEY IDENTITY(1,1),
        AllocationNo     NVARCHAR(20)  NOT NULL,
        AssetID          INT           NOT NULL,
        AllocationDate   DATE          NOT NULL,
        AllocatedToType  NVARCHAR(20)  NOT NULL,
        AllocatedToID    INT           NULL,
        AllocatedToName  NVARCHAR(200) NULL,
        BranchCode       NVARCHAR(20)  NULL,
        Remarks          NVARCHAR(500) NULL,
        IsActive         BIT           NOT NULL DEFAULT 1,
        LASTUPDATE       DATETIME      NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)  NULL,

        CONSTRAINT FK_AssetAlloc_Asset FOREIGN KEY (AssetID) REFERENCES AssetMaster(ID)
    );
    CREATE UNIQUE INDEX UX_AssetAllocation_No ON AssetAllocation(AllocationNo);
    CREATE INDEX IX_AssetAllocation_AssetID  ON AssetAllocation(AssetID);
    PRINT 'AssetAllocation table created.';
END
ELSE PRINT 'AssetAllocation already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : AssetTransfer
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetTransfer')
BEGIN
    CREATE TABLE AssetTransfer (
        ID             INT           PRIMARY KEY IDENTITY(1,1),
        TransferNo     NVARCHAR(20)  NOT NULL,
        AssetID        INT           NOT NULL,
        TransferDate   DATE          NOT NULL,
        FromType       NVARCHAR(20)  NOT NULL,
        FromID         INT           NULL,
        FromName       NVARCHAR(200) NULL,
        ToType         NVARCHAR(20)  NOT NULL,
        ToID           INT           NULL,
        ToName         NVARCHAR(200) NULL,
        TransferReason NVARCHAR(500) NULL,
        Remarks        NVARCHAR(500) NULL,
        IsActive       BIT           NOT NULL DEFAULT 1,
        LASTUPDATE     DATETIME      NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy  NVARCHAR(50)  NULL,

        CONSTRAINT FK_AssetTransfer_Asset FOREIGN KEY (AssetID) REFERENCES AssetMaster(ID)
    );
    CREATE UNIQUE INDEX UX_AssetTransfer_No ON AssetTransfer(TransferNo);
    CREATE INDEX IX_AssetTransfer_AssetID   ON AssetTransfer(AssetID);
    PRINT 'AssetTransfer table created.';
END
ELSE PRINT 'AssetTransfer already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 4 : AssetReturn
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetReturn')
BEGIN
    CREATE TABLE AssetReturn (
        ID              INT           PRIMARY KEY IDENTITY(1,1),
        ReturnNo        NVARCHAR(20)  NOT NULL,
        AssetID         INT           NOT NULL,
        AllocationID    INT           NOT NULL,
        ReturnDate      DATE          NOT NULL,
        ReturnedByType  NVARCHAR(20)  NOT NULL,
        ReturnedByID    INT           NULL,
        ReturnedByName  NVARCHAR(200) NULL,
        ReturnCondition NVARCHAR(20)  NULL,
        ReceivedBy      NVARCHAR(100) NULL,
        Remarks         NVARCHAR(500) NULL,
        IsActive        BIT           NOT NULL DEFAULT 1,
        LASTUPDATE      DATETIME      NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy   NVARCHAR(50)  NULL,

        CONSTRAINT FK_AssetReturn_Asset      FOREIGN KEY (AssetID)      REFERENCES AssetMaster(ID),
        CONSTRAINT FK_AssetReturn_Allocation FOREIGN KEY (AllocationID) REFERENCES AssetAllocation(ID)
    );
    CREATE UNIQUE INDEX UX_AssetReturn_No       ON AssetReturn(ReturnNo);
    CREATE INDEX IX_AssetReturn_AssetID         ON AssetReturn(AssetID);
    CREATE INDEX IX_AssetReturn_AllocationID    ON AssetReturn(AllocationID);
    PRINT 'AssetReturn table created.';
END
ELSE PRINT 'AssetReturn already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 5 : AssetMaintenance
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetMaintenance')
BEGIN
    CREATE TABLE AssetMaintenance (
        ID                 INT           PRIMARY KEY IDENTITY(1,1),
        MaintenanceNo      NVARCHAR(20)  NOT NULL,
        AssetID            INT           NOT NULL,
        MaintenanceType    NVARCHAR(50)  NOT NULL,
        ProblemDescription NVARCHAR(500) NULL,
        VendorName         NVARCHAR(200) NULL,
        StartDate          DATE          NOT NULL,
        CompletionDate     DATE          NULL,
        MaintenanceCost    DECIMAL(18,2) NULL DEFAULT 0,
        Status             NVARCHAR(20)  NOT NULL DEFAULT 'Scheduled',
        CompletionRemarks  NVARCHAR(500) NULL,
        Remarks            NVARCHAR(500) NULL,
        IsActive           BIT           NOT NULL DEFAULT 1,
        LASTUPDATE         DATETIME      NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy      NVARCHAR(50)  NULL,

        CONSTRAINT FK_AssetMaintenance_Asset FOREIGN KEY (AssetID) REFERENCES AssetMaster(ID)
    );
    CREATE UNIQUE INDEX UX_AssetMaintenance_No ON AssetMaintenance(MaintenanceNo);
    CREATE INDEX IX_AssetMaintenance_AssetID   ON AssetMaintenance(AssetID);
    CREATE INDEX IX_AssetMaintenance_Status    ON AssetMaintenance(Status);
    PRINT 'AssetMaintenance table created.';
END
ELSE PRINT 'AssetMaintenance already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 6 : AssetDamage
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetDamage')
BEGIN
    CREATE TABLE AssetDamage (
        ID                  INT           PRIMARY KEY IDENTITY(1,1),
        DamageNo            NVARCHAR(20)  NOT NULL,
        AssetID             INT           NOT NULL,
        DamageDate          DATE          NOT NULL,
        DamageType          NVARCHAR(50)  NULL,
        DamageDescription   NVARCHAR(500) NOT NULL,
        DamageCondition     NVARCHAR(20)  NULL,
        ReportedBy          NVARCHAR(100) NULL,
        EstimatedRepairCost DECIMAL(18,2) NULL DEFAULT 0,
        Remarks             NVARCHAR(500) NULL,
        IsActive            BIT           NOT NULL DEFAULT 1,
        LASTUPDATE          DATETIME      NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy       NVARCHAR(50)  NULL,

        CONSTRAINT FK_AssetDamage_Asset FOREIGN KEY (AssetID) REFERENCES AssetMaster(ID)
    );
    CREATE UNIQUE INDEX UX_AssetDamage_No ON AssetDamage(DamageNo);
    CREATE INDEX IX_AssetDamage_AssetID   ON AssetDamage(AssetID);
    PRINT 'AssetDamage table created.';
END
ELSE PRINT 'AssetDamage already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 7 : AssetDisposal
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetDisposal')
BEGIN
    CREATE TABLE AssetDisposal (
        ID            INT           PRIMARY KEY IDENTITY(1,1),
        DisposalNo    NVARCHAR(20)  NOT NULL,
        AssetID       INT           NOT NULL,
        DisposalDate  DATE          NOT NULL,
        DisposalType  NVARCHAR(50)  NOT NULL,
        DisposalReason NVARCHAR(500) NOT NULL,
        DisposalValue  DECIMAL(18,2) NULL DEFAULT 0,
        ApprovedBy    NVARCHAR(100) NULL,
        Remarks       NVARCHAR(500) NULL,
        IsActive      BIT           NOT NULL DEFAULT 1,
        LASTUPDATE    DATETIME      NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy NVARCHAR(50)  NULL,

        CONSTRAINT FK_AssetDisposal_Asset FOREIGN KEY (AssetID) REFERENCES AssetMaster(ID)
    );
    CREATE UNIQUE INDEX UX_AssetDisposal_No ON AssetDisposal(DisposalNo);
    CREATE INDEX IX_AssetDisposal_AssetID   ON AssetDisposal(AssetID);
    PRINT 'AssetDisposal table created.';
END
ELSE PRINT 'AssetDisposal already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 8 : AssetMovementHistory
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetMovementHistory')
BEGIN
    CREATE TABLE AssetMovementHistory (
        ID             INT           PRIMARY KEY IDENTITY(1,1),
        AssetID        INT           NOT NULL,
        MovementDate   DATETIME      NOT NULL DEFAULT GETDATE(),
        MovementType   NVARCHAR(30)  NOT NULL,
        FromLocation   NVARCHAR(200) NULL,
        ToLocation     NVARCHAR(200) NULL,
        FromPerson     NVARCHAR(200) NULL,
        ToPerson       NVARCHAR(200) NULL,
        ReferenceNo    NVARCHAR(20)  NULL,
        ReferenceType  NVARCHAR(30)  NULL,
        ReferenceID    INT           NULL,
        PreviousStatus NVARCHAR(30)  NULL,
        NewStatus      NVARCHAR(30)  NULL,
        Remarks        NVARCHAR(500) NULL,
        CreatedBy      NVARCHAR(50)  NULL,
        LASTUPDATE     DATETIME      NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_AssetMovement_Asset FOREIGN KEY (AssetID) REFERENCES AssetMaster(ID)
    );
    CREATE INDEX IX_AssetMovement_AssetID      ON AssetMovementHistory(AssetID);
    CREATE INDEX IX_AssetMovement_MovementDate ON AssetMovementHistory(MovementDate);
    CREATE INDEX IX_AssetMovement_MovementType ON AssetMovementHistory(MovementType);
    PRINT 'AssetMovementHistory table created.';
END
ELSE PRINT 'AssetMovementHistory already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 9 : ScreenList entries
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Master')
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy) VALUES ('Asset Master', 'Inventory', GETDATE(), 'MIGRATION');

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Allocation')
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy) VALUES ('Asset Allocation', 'Inventory', GETDATE(), 'MIGRATION');

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Transfer')
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy) VALUES ('Asset Transfer', 'Inventory', GETDATE(), 'MIGRATION');

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Return')
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy) VALUES ('Asset Return', 'Inventory', GETDATE(), 'MIGRATION');

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Maintenance')
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy) VALUES ('Asset Maintenance', 'Inventory', GETDATE(), 'MIGRATION');

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Damage')
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy) VALUES ('Asset Damage', 'Inventory', GETDATE(), 'MIGRATION');

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Disposal')
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy) VALUES ('Asset Disposal', 'Inventory', GETDATE(), 'MIGRATION');

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Movement History')
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy) VALUES ('Asset Movement History', 'Inventory', GETDATE(), 'MIGRATION');

PRINT 'ScreenList: All Part 6 entries verified.';
GO

PRINT '============================================================';
PRINT 'OBMS Part 6 Asset Management Migration - Completed.';
PRINT '============================================================';
GO
