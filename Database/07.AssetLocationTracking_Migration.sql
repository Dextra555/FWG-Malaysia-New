-- ============================================================
-- OBMS Asset Location Tracking Migration
-- Track where company vehicles/assets are currently parked
-- especially when Guards park at unexpected locations.
--
-- Changes:
--   1. Add 3 new columns to AssetMaster
--   2. Create AssetLocationLog table
--   3. ScreenList entry
--
-- IDEMPOTENT — safe to re-run.
-- ============================================================

PRINT '============================================================';
PRINT 'Asset Location Tracking Migration - Starting...';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : Add location columns to AssetMaster
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='CurrentLocation')
BEGIN
    ALTER TABLE AssetMaster ADD CurrentLocation NVARCHAR(500) NULL;
    PRINT 'AssetMaster: CurrentLocation added.';
END
ELSE PRINT 'AssetMaster: CurrentLocation already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='LocationStatus')
BEGIN
    ALTER TABLE AssetMaster ADD LocationStatus NVARCHAR(30) NULL;
    -- Values: Normal | Defect_Location | Parked_Away | Unknown
    PRINT 'AssetMaster: LocationStatus added.';
END
ELSE PRINT 'AssetMaster: LocationStatus already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='LocationUpdatedBy')
BEGIN
    ALTER TABLE AssetMaster ADD LocationUpdatedBy NVARCHAR(100) NULL;
    PRINT 'AssetMaster: LocationUpdatedBy added.';
END
ELSE PRINT 'AssetMaster: LocationUpdatedBy already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='LocationUpdatedDate')
BEGIN
    ALTER TABLE AssetMaster ADD LocationUpdatedDate DATETIME NULL;
    PRINT 'AssetMaster: LocationUpdatedDate added.';
END
ELSE PRINT 'AssetMaster: LocationUpdatedDate already exists.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : AssetLocationLog
-- Every location update appends one row.
-- Never updated — full history is preserved.
--
-- LocationStatus values:
--   Normal         — car is at expected/base location
--   Defect_Location — car parked at wrong/unexpected location
--   Parked_Away    — Guard parked away from duty point
--   Unknown        — location not confirmed
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetLocationLog')
BEGIN
    CREATE TABLE AssetLocationLog (
        ID               INT            PRIMARY KEY IDENTITY(1,1),
        AssetID          INT            NOT NULL,
        LogDate          DATETIME       NOT NULL DEFAULT GETDATE(),
        PreviousLocation NVARCHAR(500)  NULL,
        NewLocation      NVARCHAR(500)  NOT NULL,
        LocationStatus   NVARCHAR(30)   NOT NULL DEFAULT 'Normal',
        UpdatedBy        NVARCHAR(100)  NULL,   -- Guard / staff who reported
        Remarks          NVARCHAR(500)  NULL,
        LASTUPDATE       DATETIME       NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy    NVARCHAR(50)   NULL,

        CONSTRAINT FK_AssetLocLog_Asset FOREIGN KEY (AssetID) REFERENCES AssetMaster(ID)
    );
    CREATE INDEX IX_AssetLocLog_AssetID  ON AssetLocationLog(AssetID);
    CREATE INDEX IX_AssetLocLog_LogDate  ON AssetLocationLog(LogDate);
    CREATE INDEX IX_AssetLocLog_Status   ON AssetLocationLog(LocationStatus);
    PRINT 'AssetLocationLog table created.';
END
ELSE PRINT 'AssetLocationLog already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : ScreenList entry
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Location')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Location', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Asset Location added.';
END
ELSE PRINT 'ScreenList: Asset Location already exists.';
GO

PRINT '============================================================';
PRINT 'Asset Location Tracking Migration - Completed.';
PRINT '============================================================';
GO
