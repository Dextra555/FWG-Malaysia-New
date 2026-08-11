-- ============================================================
-- Credit Note and Debit Note Tables Migration
-- Run this script on the OBMS database
-- ============================================================

-- ─── CreditNote Table ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CreditNote')
BEGIN
    CREATE TABLE CreditNote (
        ID                  INT             PRIMARY KEY IDENTITY(1,1),
        CreditNoteNo        NVARCHAR(50)    NOT NULL,
        Branch              NVARCHAR(20)    NOT NULL,
        Client              NVARCHAR(50)    NOT NULL,
        AgreementID         INT             NULL,
        ClientInvoiceID     INT             NULL,
        CreditNoteDate      DATETIME        NOT NULL,
        CreditNoteAmount    DECIMAL(18,2)   NOT NULL,
        Reason              NVARCHAR(1000)  NULL,
        ReferenceInvoiceNo  NVARCHAR(50)    NULL,
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
        ApprovalDate        DATETIME        NULL,
        ApprovedBy          NVARCHAR(50)    NULL,
        CreatedDate         DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedDate     DATETIME        NULL,
        LastUpdatedBy       NVARCHAR(50)    NULL,
        IsDeleted           BIT             NOT NULL DEFAULT 0
    );

    -- Indexes
    CREATE UNIQUE INDEX UX_CreditNote_CreditNoteNo
        ON CreditNote (CreditNoteNo)
        WHERE IsDeleted = 0;

    CREATE INDEX IX_CreditNote_Client_Branch
        ON CreditNote (Client, Branch);

    CREATE INDEX IX_CreditNote_AgreementID
        ON CreditNote (AgreementID)
        WHERE AgreementID IS NOT NULL;

    CREATE INDEX IX_CreditNote_ClientInvoiceID
        ON CreditNote (ClientInvoiceID)
        WHERE ClientInvoiceID IS NOT NULL;

    PRINT 'CreditNote table created successfully.';
END
ELSE
    PRINT 'CreditNote table already exists - skipped.';
GO

-- ─── DebitNote Table ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DebitNote')
BEGIN
    CREATE TABLE DebitNote (
        ID                  INT             PRIMARY KEY IDENTITY(1,1),
        DebitNoteNo         NVARCHAR(50)    NOT NULL,
        Branch              NVARCHAR(20)    NOT NULL,
        Client              NVARCHAR(50)    NOT NULL,
        AgreementID         INT             NULL,
        ClientInvoiceID     INT             NULL,
        DebitNoteDate       DATETIME        NOT NULL,
        DebitNoteAmount     DECIMAL(18,2)   NOT NULL,
        Reason              NVARCHAR(1000)  NULL,
        ReferenceInvoiceNo  NVARCHAR(50)    NULL,
        Status              NVARCHAR(20)    NOT NULL DEFAULT 'Pending',
        ApprovalDate        DATETIME        NULL,
        ApprovedBy          NVARCHAR(50)    NULL,
        DueDate             DATETIME        NULL,
        CreatedDate         DATETIME        NOT NULL DEFAULT GETDATE(),
        LastUpdatedDate     DATETIME        NULL,
        LastUpdatedBy       NVARCHAR(50)    NULL,
        IsDeleted           BIT             NOT NULL DEFAULT 0
    );

    -- Indexes
    CREATE UNIQUE INDEX UX_DebitNote_DebitNoteNo
        ON DebitNote (DebitNoteNo)
        WHERE IsDeleted = 0;

    CREATE INDEX IX_DebitNote_Client_Branch
        ON DebitNote (Client, Branch);

    CREATE INDEX IX_DebitNote_AgreementID
        ON DebitNote (AgreementID)
        WHERE AgreementID IS NOT NULL;

    CREATE INDEX IX_DebitNote_ClientInvoiceID
        ON DebitNote (ClientInvoiceID)
        WHERE ClientInvoiceID IS NOT NULL;

    PRINT 'DebitNote table created successfully.';
END
ELSE
    PRINT 'DebitNote table already exists - skipped.';
GO

-- ─── Optional: Migrate existing CreditNoteAmount from Receipts ──
-- Uncomment the block below ONLY if you want to create historical
-- CreditNote records from existing Receipt data.
--
-- INSERT INTO CreditNote (CreditNoteNo, Branch, Client, CreditNoteDate,
--     CreditNoteAmount, Reason, Status, CreatedDate, LastUpdatedBy)
-- SELECT
--     'CN-MIG-' + CAST(ID AS NVARCHAR(10)),
--     Branch,
--     PaymentFrom,           -- adjust to your actual client column
--     ReceiptDate,
--     CreditNoteAmount,
--     'Migrated from Receipts',
--     'Applied',
--     GETDATE(),
--     'MIGRATION'
-- FROM Receipts
-- WHERE CreditNoteAmount > 0 AND IsDeleted = 0;
--
-- PRINT 'Credit note migration from Receipts completed.';
-- GO

-- ============================================================
-- Add CreditNote & DebitNote to ScreenList
-- This makes them appear in Administration > User Access Rights
-- under the "Finance" category tab
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'CreditNote')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('CreditNote', 'Finance', GETDATE(), 'MIGRATION');
    PRINT 'CreditNote added to ScreenList.';
END
ELSE
    PRINT 'CreditNote already in ScreenList - skipped.';
GO

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'DebitNote')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('DebitNote', 'Finance', GETDATE(), 'MIGRATION');
    PRINT 'DebitNote added to ScreenList.';
END
ELSE
    PRINT 'DebitNote already in ScreenList - skipped.';
GO

-- Verify
SELECT ScreenName, Category FROM ScreenList 
WHERE Category = 'Finance' 
ORDER BY ScreenName;
GO

-- ============================================================
-- Add Tax columns to CreditNote and DebitNote
-- Remove Status and AgreementID columns (with data safety)
-- ============================================================

-- ─── CreditNote: Add new Tax columns ─────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CreditNote' AND COLUMN_NAME='TaxPercentage')
    ALTER TABLE CreditNote ADD TaxPercentage DECIMAL(18,2) NOT NULL DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CreditNote' AND COLUMN_NAME='TaxAmount')
    ALTER TABLE CreditNote ADD TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='CreditNote' AND COLUMN_NAME='TotalAmount')
    ALTER TABLE CreditNote ADD TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
GO

-- ─── DebitNote: Add new Tax columns ──────────────────────────
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='DebitNote' AND COLUMN_NAME='TaxPercentage')
    ALTER TABLE DebitNote ADD TaxPercentage DECIMAL(18,2) NOT NULL DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='DebitNote' AND COLUMN_NAME='TaxAmount')
    ALTER TABLE DebitNote ADD TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='DebitNote' AND COLUMN_NAME='TotalAmount')
    ALTER TABLE DebitNote ADD TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
GO

-- ─── Update existing TotalAmount from CreditNoteAmount (backfill) ─
UPDATE CreditNote SET TotalAmount = CreditNoteAmount WHERE TotalAmount = 0;
UPDATE DebitNote   SET TotalAmount = DebitNoteAmount   WHERE TotalAmount = 0;
GO

PRINT 'Tax columns added to CreditNote and DebitNote successfully.';
GO
