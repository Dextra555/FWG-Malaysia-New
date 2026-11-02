-- Migration: Create ClientBreak table for unlimited breaks per client
-- Date: 2026-10-21
-- Replaces the fixed Break1-Break5 column approach on ClientMaster

-- ── Step 1: Remove Break1-Break5 columns from ClientMaster (if they exist) ──
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break1')
    ALTER TABLE ClientMaster DROP COLUMN Break1;

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break2')
    ALTER TABLE ClientMaster DROP COLUMN Break2;

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break3')
    ALTER TABLE ClientMaster DROP COLUMN Break3;

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break4')
    ALTER TABLE ClientMaster DROP COLUMN Break4;

IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ClientMaster' AND COLUMN_NAME = 'Break5')
    ALTER TABLE ClientMaster DROP COLUMN Break5;

PRINT 'Break1-Break5 columns removed from ClientMaster (if they existed).';

-- ── Step 2: Create ClientBreak table ─────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'ClientBreak'
)
BEGIN
    CREATE TABLE ClientBreak (
        ID          INT            IDENTITY(1,1) PRIMARY KEY,
        ClientCode  VARCHAR(20)    NOT NULL,    -- matches ClientMaster.Code (varchar 20)
        BreakHours  DECIMAL(3,1)   NOT NULL,   -- 0.5 / 1.0 / 1.5 / 2.0
        SortOrder   INT            NOT NULL DEFAULT 0,
        CreatedDate DATETIME       NULL DEFAULT GETDATE(),
        CONSTRAINT FK_ClientBreak_ClientMaster
            FOREIGN KEY (ClientCode) REFERENCES ClientMaster(Code)
    );
    PRINT 'Table ClientBreak created successfully.';
END
ELSE
BEGIN
    PRINT 'Table ClientBreak already exists. Skipping.';
END
