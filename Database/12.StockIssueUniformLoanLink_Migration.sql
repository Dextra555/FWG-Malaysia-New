-- ============================================================
-- Migration: StockIssueUniformLoanLink
-- Purpose  : Tracks which StockIssueDoc has been linked to an
--            active SalaryAdvance (TransType=4, Uniform Loan).
--            Prevents the same Stock Issue from being linked to
--            multiple active loans.
-- Created  : 2026-10-05
-- ============================================================

-- 1. Create the link table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'StockIssueUniformLoanLink'
)
BEGIN
    CREATE TABLE [dbo].[StockIssueUniformLoanLink] (
        [ID]              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        [StockIssueDocID] INT            NOT NULL,   -- FK → StockIssueDoc.ID
        [SalaryAdvanceID] INT            NOT NULL,   -- FK → SalaryAdvance.ID (TransType=4)
        [LinkedDate]      DATETIME       NOT NULL DEFAULT GETDATE(),
        [LinkedBy]        NVARCHAR(100)  NULL,
        [IsActive]        BIT            NOT NULL DEFAULT 1,
        [LASTUPDATE]      DATETIME       NOT NULL DEFAULT GETDATE(),
        [LastUpdatedBy]   NVARCHAR(100)  NULL
    );

    -- Index to speed up "is this issue already linked?" lookup
    CREATE UNIQUE INDEX UX_StockIssueUniformLoanLink_IssueActive
        ON [dbo].[StockIssueUniformLoanLink] ([StockIssueDocID])
        WHERE [IsActive] = 1;

    PRINT 'StockIssueUniformLoanLink table created.';
END
ELSE
    PRINT 'StockIssueUniformLoanLink table already exists — skipped.';

-- 2. FK constraints (added separately so they can be re-run safely)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_NAME = 'FK_SIULL_StockIssueDoc'
)
BEGIN
    ALTER TABLE [dbo].[StockIssueUniformLoanLink]
        ADD CONSTRAINT FK_SIULL_StockIssueDoc
        FOREIGN KEY ([StockIssueDocID]) REFERENCES [dbo].[StockIssueDoc]([ID]);
    PRINT 'FK_SIULL_StockIssueDoc added.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_NAME = 'FK_SIULL_SalaryAdvance'
)
BEGIN
    ALTER TABLE [dbo].[StockIssueUniformLoanLink]
        ADD CONSTRAINT FK_SIULL_SalaryAdvance
        FOREIGN KEY ([SalaryAdvanceID]) REFERENCES [dbo].[SalaryAdvance]([ID]);
    PRINT 'FK_SIULL_SalaryAdvance added.';
END

PRINT 'Migration 12 complete.';
