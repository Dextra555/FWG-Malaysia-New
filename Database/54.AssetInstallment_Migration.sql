-- ═══════════════════════════════════════════════════════════════════════════
-- Migration 54 : Asset Deposit & Installment Payment
-- Purpose      : 
--   1. Add DepositAmount, PaymentTermType, InstallmentStartDate,
--      MonthlyInstallmentAmount, InstallmentMonths to AssetMaster.
--   2. Create AssetInstallment table — one row per monthly due.
--   3. Each installment row links to a CreditorInvoice (InvoiceType='AI')
--      so it appears in Finance → Payment → Purchase (GRN mode now includes
--      asset installments via GetAssetInstallmentsForPayment endpoint).
-- 
-- Run once against OBMSUATNEW.
-- ═══════════════════════════════════════════════════════════════════════════

-- ─────────────────────────────────────────────────────────────────────────
-- SECTION 1 : Add new columns to AssetMaster
-- ─────────────────────────────────────────────────────────────────────────

-- Deposit Amount (reference/dummy field — for display only, not tracked in payments)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='DepositAmount')
BEGIN
    ALTER TABLE AssetMaster ADD DepositAmount DECIMAL(18,2) NULL;
    PRINT 'AssetMaster: DepositAmount added.';
END
ELSE PRINT 'AssetMaster: DepositAmount already exists.';
GO

-- Payment Term Type: FULL or MONTHLY
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='PaymentTermType')
BEGIN
    ALTER TABLE AssetMaster ADD PaymentTermType NVARCHAR(10) NULL DEFAULT 'FULL';
    -- FULL   = single full payment (no installments generated)
    -- MONTHLY = split into monthly installments
    PRINT 'AssetMaster: PaymentTermType added.';
END
ELSE PRINT 'AssetMaster: PaymentTermType already exists.';
GO

-- Installment Start Date (first month of payment)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='InstallmentStartDate')
BEGIN
    ALTER TABLE AssetMaster ADD InstallmentStartDate DATE NULL;
    PRINT 'AssetMaster: InstallmentStartDate added.';
END
ELSE PRINT 'AssetMaster: InstallmentStartDate already exists.';
GO

-- Monthly installment amount (manual entry; used for display & to generate rows)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='MonthlyInstallmentAmount')
BEGIN
    ALTER TABLE AssetMaster ADD MonthlyInstallmentAmount DECIMAL(18,2) NULL;
    PRINT 'AssetMaster: MonthlyInstallmentAmount added.';
END
ELSE PRINT 'AssetMaster: MonthlyInstallmentAmount already exists.';
GO

-- Number of installment months (auto-calculated: (PurchaseAmount - DepositAmount) / Monthly)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='InstallmentMonths')
BEGIN
    ALTER TABLE AssetMaster ADD InstallmentMonths INT NULL;
    PRINT 'AssetMaster: InstallmentMonths added.';
END
ELSE PRINT 'AssetMaster: InstallmentMonths already exists.';
GO

-- ─────────────────────────────────────────────────────────────────────────
-- SECTION 2 : AssetInstallment table
-- One row per monthly installment for each Asset that uses MONTHLY payment term.
-- Each row has a linked CreditorInvoice (InvoiceType='AI') so Finance → Payment
-- can pick it up in the GRN/Asset payment flow.
-- ─────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AssetInstallment')
BEGIN
    CREATE TABLE AssetInstallment (
        ID                  INT            PRIMARY KEY IDENTITY(1,1),
        AssetID             INT            NOT NULL,
        InstallmentNo       INT            NOT NULL,         -- 1, 2, 3 ...
        DueDate             DATE           NOT NULL,         -- 1st of each month
        Amount              DECIMAL(18,2)  NOT NULL,
        Status              NVARCHAR(10)   NOT NULL DEFAULT 'Pending',
        -- Status: Pending | Paid
        CreditorInvoiceID   INT            NULL,             -- FK to CreditorInvoice after creation
        PaymentID           INT            NULL,             -- set when paid via Finance Payment
        IsActive            BIT            NOT NULL DEFAULT 1,
        LASTUPDATE          DATETIME       NOT NULL DEFAULT GETDATE(),
        LastUpdatedBy       NVARCHAR(50)   NULL,

        CONSTRAINT FK_AssetInstallment_Asset FOREIGN KEY (AssetID) REFERENCES AssetMaster(ID)
    );
    CREATE INDEX IX_AssetInstallment_AssetID  ON AssetInstallment(AssetID);
    CREATE INDEX IX_AssetInstallment_DueDate  ON AssetInstallment(DueDate);
    CREATE INDEX IX_AssetInstallment_Status   ON AssetInstallment(Status);
    PRINT 'AssetInstallment table created.';
END
ELSE PRINT 'AssetInstallment already exists. Skipping.';
GO

-- ─────────────────────────────────────────────────────────────────────────
-- SECTION 3 : ScreenList entry
-- ─────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Asset Installment')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Asset Installment', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Asset Installment added.';
END
ELSE PRINT 'ScreenList: Asset Installment already exists.';
GO

PRINT '============================================================';
PRINT 'Migration 54 - Asset Installment - Completed.';
PRINT '============================================================';
GO

-- ─────────────────────────────────────────────────────────────────────────
-- SECTION 4 : Add SupplierInvoiceNo to AssetMaster
-- Added after initial migration — safe to re-run (IF NOT EXISTS guarded).
-- ─────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='SupplierInvoiceNo')
BEGIN
    ALTER TABLE AssetMaster ADD SupplierInvoiceNo NVARCHAR(100) NULL;
    PRINT 'AssetMaster: SupplierInvoiceNo added.';
END
ELSE PRINT 'AssetMaster: SupplierInvoiceNo already exists.';
GO

-- ─────────────────────────────────────────────────────────────────────────
-- SECTION 5 : Update SupplierInvoiceView — Exclude Asset Installment rows
-- Asset Installment CreditorInvoice rows (InvoiceType='A') must NOT appear
-- in the Expenses payment flow. They are handled via Purchase → Asset Inst.
-- ─────────────────────────────────────────────────────────────────────────

ALTER VIEW [dbo].[SupplierInvoiceView] AS
SELECT DISTINCT
    TOP (100) PERCENT
    dbo.CreditorInvoice.ID,
    dbo.BranchMaster.Name          AS Branchname,
    dbo.Suppliers.Name             AS Suppliername,
    dbo.CreditorInvoice.InvoiceNo,
    dbo.CreditorInvoice.PaymentDate AS Dudate,
    dbo.CreditorInvoice.Total,
    ISNULL(dbo.PaidAmount(dbo.CreditorInvoice.ID), 0) AS Amount,
    dbo.OBMSBranches.Name          AS UserId
FROM dbo.CreditorInvoice
INNER JOIN dbo.Suppliers
    ON dbo.CreditorInvoice.Supplier    = dbo.Suppliers.Id
INNER JOIN dbo.BranchMaster
    ON dbo.CreditorInvoice.Branch      = dbo.BranchMaster.Code
INNER JOIN dbo.OBMSBranches
    ON dbo.BranchMaster.Code           = dbo.OBMSBranches.BranchCode
WHERE
    YEAR(dbo.CreditorInvoice.PaymentDate) >= 2014
    AND dbo.CreditorInvoice.PaymentDate  <= GETDATE()
    AND dbo.CreditorInvoice.Total
            > ISNULL(dbo.PaidAmount(dbo.CreditorInvoice.ID), 0)
    AND dbo.CreditorInvoice.IsDeleted    = 0
    AND dbo.CreditorInvoice.InvoiceType NOT IN ('G', 'A')  -- Exclude GRN and Asset Installment rows
ORDER BY dbo.CreditorInvoice.ID;
GO

-- ─────────────────────────────────────────────────────────────────────────
-- SECTION 6 : Add Preclosing columns to AssetMaster
-- ─────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='IsPreClosing')
BEGIN
    ALTER TABLE AssetMaster ADD IsPreClosing BIT NOT NULL
        CONSTRAINT DF_AssetMaster_IsPreClosing DEFAULT 0;
    PRINT 'AssetMaster: IsPreClosing added.';
END
ELSE PRINT 'AssetMaster: IsPreClosing already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='PreClosingDate')
BEGIN
    ALTER TABLE AssetMaster ADD PreClosingDate DATE NULL;
    PRINT 'AssetMaster: PreClosingDate added.';
END
ELSE PRINT 'AssetMaster: PreClosingDate already exists.';
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME='AssetMaster' AND COLUMN_NAME='PreClosingAmount')
BEGIN
    ALTER TABLE AssetMaster ADD PreClosingAmount DECIMAL(18,2) NULL;
    PRINT 'AssetMaster: PreClosingAmount added.';
END
ELSE PRINT 'AssetMaster: PreClosingAmount already exists.';
GO
