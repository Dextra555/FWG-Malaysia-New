-- ============================================================
-- OBMS Inventory Master - Complete Migration Script
-- Run this ONCE on any database (local or live) to apply
-- all Part 1 Inventory changes.
--
-- This script is IDEMPOTENT:
--   - Already existing tables are skipped
--   - Already existing columns are skipped
--   - Already existing rows are skipped
--   - Safe to re-run without creating duplicates
--
-- Target database : OBMSUATNEW (or live equivalent)
-- Last updated    : 2026
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Inventory Master Migration - Starting...';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- SECTION 1 : NEW MASTER TABLES
-- ─────────────────────────────────────────────────────────────

-- ─── BrandMaster ────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BrandMaster')
BEGIN
    CREATE TABLE BrandMaster (
        ID            INT          PRIMARY KEY IDENTITY(1,1),
        Code          NVARCHAR(50) NOT NULL,
        Name          NVARCHAR(200) NOT NULL,
        Description   NVARCHAR(500) NULL,
        IsActive      BIT          NOT NULL DEFAULT 1,
        LASTUPDATE    DATETIME     NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy NVARCHAR(50) NULL
    );
    CREATE UNIQUE INDEX UX_BrandMaster_Code ON BrandMaster(Code);
    CREATE        INDEX IX_BrandMaster_Name ON BrandMaster(Name);
    PRINT 'BrandMaster table created.';
END
ELSE
    PRINT 'BrandMaster already exists. Skipping.';

-- ─── UOMMaster ──────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UOMMaster')
BEGIN
    CREATE TABLE UOMMaster (
        ID            INT          PRIMARY KEY IDENTITY(1,1),
        Code          NVARCHAR(50) NOT NULL,
        Name          NVARCHAR(200) NOT NULL,
        ShortName     NVARCHAR(50) NULL,
        IsPrimary     BIT          NOT NULL DEFAULT 0,
        LASTUPDATE    DATETIME     NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy NVARCHAR(50) NULL
    );
    CREATE UNIQUE INDEX UX_UOMMaster_Code ON UOMMaster(Code);
    CREATE        INDEX IX_UOMMaster_Name ON UOMMaster(Name);
    PRINT 'UOMMaster table created.';
END
ELSE
    PRINT 'UOMMaster already exists. Skipping.';

-- ─── WarehouseMaster ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WarehouseMaster')
BEGIN
    CREATE TABLE WarehouseMaster (
        ID            INT          PRIMARY KEY IDENTITY(1,1),
        Code          NVARCHAR(50) NOT NULL,
        Name          NVARCHAR(200) NOT NULL,
        Address       NVARCHAR(500) NULL,
        BranchID      INT          NOT NULL,
        IsActive      BIT          NOT NULL DEFAULT 1,
        LASTUPDATE    DATETIME     NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy NVARCHAR(50) NULL
    );
    CREATE UNIQUE INDEX UX_WarehouseMaster_Code ON WarehouseMaster(Code);
    CREATE        INDEX IX_WarehouseMaster_Name ON WarehouseMaster(Name);
    CREATE        INDEX IX_WarehouseMaster_BranchID ON WarehouseMaster(BranchID);
    PRINT 'WarehouseMaster table created.';
END
ELSE
    PRINT 'WarehouseMaster already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 2 : EXTEND ITEMMASTER TABLE
-- ─────────────────────────────────────────────────────────────

-- Code (auto-generated item code, e.g. ITM0001)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ItemMaster' AND COLUMN_NAME = 'Code')
BEGIN
    ALTER TABLE ItemMaster ADD Code NVARCHAR(50) NULL;
    PRINT 'ItemMaster.Code column added.';
END
ELSE
    PRINT 'ItemMaster.Code already exists. Skipping.';

-- Backfill Code for rows that do not have one yet
UPDATE ItemMaster
SET    Code = 'ITM' + RIGHT('0000' + CAST(ID AS VARCHAR(4)), 4)
WHERE  Code IS NULL;
PRINT 'ItemMaster.Code backfill done (rows without code updated).';

-- BrandID  (FK -> BrandMaster)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ItemMaster' AND COLUMN_NAME = 'BrandID')
BEGIN
    ALTER TABLE ItemMaster ADD BrandID INT NULL;
    ALTER TABLE ItemMaster ADD CONSTRAINT FK_ItemMaster_BrandMaster
        FOREIGN KEY (BrandID) REFERENCES BrandMaster(ID);
    PRINT 'ItemMaster.BrandID column added.';
END
ELSE
    PRINT 'ItemMaster.BrandID already exists. Skipping.';

-- UOMID  (FK -> UOMMaster)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ItemMaster' AND COLUMN_NAME = 'UOMID')
BEGIN
    ALTER TABLE ItemMaster ADD UOMID INT NULL;
    ALTER TABLE ItemMaster ADD CONSTRAINT FK_ItemMaster_UOMMaster
        FOREIGN KEY (UOMID) REFERENCES UOMMaster(ID);
    PRINT 'ItemMaster.UOMID column added.';
END
ELSE
    PRINT 'ItemMaster.UOMID already exists. Skipping.';

-- ReorderLevel
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ItemMaster' AND COLUMN_NAME = 'ReorderLevel')
BEGIN
    ALTER TABLE ItemMaster ADD ReorderLevel DECIMAL(18,2) NULL;
    PRINT 'ItemMaster.ReorderLevel column added.';
END
ELSE
    PRINT 'ItemMaster.ReorderLevel already exists. Skipping.';

-- ReorderQuantity
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'ItemMaster' AND COLUMN_NAME = 'ReorderQuantity')
BEGIN
    ALTER TABLE ItemMaster ADD ReorderQuantity DECIMAL(18,2) NULL;
    PRINT 'ItemMaster.ReorderQuantity column added.';
END
ELSE
    PRINT 'ItemMaster.ReorderQuantity already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 3 : EXTEND INVENTORYCATEGORY TABLE
-- ─────────────────────────────────────────────────────────────

-- ParentCategoryID (self-referential FK for sub-category hierarchy)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'InventoryCategory' AND COLUMN_NAME = 'ParentCategoryID')
BEGIN
    ALTER TABLE InventoryCategory ADD ParentCategoryID INT NULL;
    ALTER TABLE InventoryCategory ADD CONSTRAINT FK_InventoryCategory_Parent
        FOREIGN KEY (ParentCategoryID) REFERENCES InventoryCategory(ID);
    PRINT 'InventoryCategory.ParentCategoryID column added.';
END
ELSE
    PRINT 'InventoryCategory.ParentCategoryID already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 4 : SCREENLIST ENTRIES
-- (Required so that User Access Rights UI shows these screens)
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Brand Master')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Brand Master', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Brand Master added.';
END
ELSE
    PRINT 'ScreenList: Brand Master already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'UOM Master')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('UOM Master', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: UOM Master added.';
END
ELSE
    PRINT 'ScreenList: UOM Master already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Warehouse Master')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Warehouse Master', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Warehouse Master added.';
END
ELSE
    PRINT 'ScreenList: Warehouse Master already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
-- SECTION 5 : UOM DEFAULT SEED DATA
-- (9 common units - skipped if Name already exists)
-- ─────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM UOMMaster WHERE Name = 'Nos')
BEGIN
    INSERT INTO UOMMaster (Code, Name, ShortName, IsPrimary, LASTUPDATE, LastUpdatedBy)
    VALUES ('UM0001', 'Nos', 'Nos', 1, GETDATE(), 'SEED');
    PRINT 'UOM seed: Nos inserted.';
END
ELSE
    PRINT 'UOM seed: Nos already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM UOMMaster WHERE Name = 'Pair')
BEGIN
    INSERT INTO UOMMaster (Code, Name, ShortName, IsPrimary, LASTUPDATE, LastUpdatedBy)
    VALUES ('UM0002', 'Pair', 'Pr', 0, GETDATE(), 'SEED');
    PRINT 'UOM seed: Pair inserted.';
END
ELSE
    PRINT 'UOM seed: Pair already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM UOMMaster WHERE Name = 'Box')
BEGIN
    INSERT INTO UOMMaster (Code, Name, ShortName, IsPrimary, LASTUPDATE, LastUpdatedBy)
    VALUES ('UM0003', 'Box', 'Box', 0, GETDATE(), 'SEED');
    PRINT 'UOM seed: Box inserted.';
END
ELSE
    PRINT 'UOM seed: Box already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM UOMMaster WHERE Name = 'Set')
BEGIN
    INSERT INTO UOMMaster (Code, Name, ShortName, IsPrimary, LASTUPDATE, LastUpdatedBy)
    VALUES ('UM0004', 'Set', 'Set', 0, GETDATE(), 'SEED');
    PRINT 'UOM seed: Set inserted.';
END
ELSE
    PRINT 'UOM seed: Set already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM UOMMaster WHERE Name = 'Kg')
BEGIN
    INSERT INTO UOMMaster (Code, Name, ShortName, IsPrimary, LASTUPDATE, LastUpdatedBy)
    VALUES ('UM0005', 'Kg', 'Kg', 0, GETDATE(), 'SEED');
    PRINT 'UOM seed: Kg inserted.';
END
ELSE
    PRINT 'UOM seed: Kg already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM UOMMaster WHERE Name = 'Gram')
BEGIN
    INSERT INTO UOMMaster (Code, Name, ShortName, IsPrimary, LASTUPDATE, LastUpdatedBy)
    VALUES ('UM0006', 'Gram', 'g', 0, GETDATE(), 'SEED');
    PRINT 'UOM seed: Gram inserted.';
END
ELSE
    PRINT 'UOM seed: Gram already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM UOMMaster WHERE Name = 'Litre')
BEGIN
    INSERT INTO UOMMaster (Code, Name, ShortName, IsPrimary, LASTUPDATE, LastUpdatedBy)
    VALUES ('UM0007', 'Litre', 'L', 0, GETDATE(), 'SEED');
    PRINT 'UOM seed: Litre inserted.';
END
ELSE
    PRINT 'UOM seed: Litre already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM UOMMaster WHERE Name = 'Millilitre')
BEGIN
    INSERT INTO UOMMaster (Code, Name, ShortName, IsPrimary, LASTUPDATE, LastUpdatedBy)
    VALUES ('UM0008', 'Millilitre', 'mL', 0, GETDATE(), 'SEED');
    PRINT 'UOM seed: Millilitre inserted.';
END
ELSE
    PRINT 'UOM seed: Millilitre already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM UOMMaster WHERE Name = 'Meter')
BEGIN
    -- Use next available code to avoid unique index conflict
    DECLARE @MeterCode NVARCHAR(10);
    SELECT @MeterCode = 'UM' + RIGHT('0000' + CAST(
        ISNULL(MAX(CAST(SUBSTRING(Code, 3, 4) AS INT)), 0) + 1
    AS VARCHAR(4)), 4)
    FROM UOMMaster WHERE Code LIKE 'UM[0-9][0-9][0-9][0-9]';

    INSERT INTO UOMMaster (Code, Name, ShortName, IsPrimary, LASTUPDATE, LastUpdatedBy)
    VALUES (@MeterCode, 'Meter', 'm', 0, GETDATE(), 'SEED');
    PRINT 'UOM seed: Meter inserted.';
END
ELSE
    PRINT 'UOM seed: Meter already exists. Skipping.';


-- ─────────────────────────────────────────────────────────────
PRINT '============================================================';
PRINT 'OBMS Inventory Master Migration - Completed Successfully.';
PRINT '============================================================';
