-- ============================================================
-- Migration 16: Stock Adjustment Approval Workflow
-- Adds Status, SubmittedBy, SubmittedDate, ApprovedBy,
-- ApprovedDate, ApprovalRemarks to StockAdjustment table.
--
-- IMPORTANT: Stock ledger entries are now created ONLY when
-- the adjustment is Approved — not at Save time.
-- Existing records (pre-migration) are stamped Status='Approved'
-- so the stock ledger rows they already created remain valid.
-- ============================================================

-- 1. Add new columns
ALTER TABLE StockAdjustment ADD
    [Status]          NVARCHAR(20)   NOT NULL DEFAULT 'Draft',
    [SubmittedBy]     NVARCHAR(100)  NULL,
    [SubmittedDate]   DATETIME       NULL,
    [ApprovedBy]      NVARCHAR(100)  NULL,
    [ApprovedDate]    DATETIME       NULL,
    [ApprovalRemarks] NVARCHAR(500)  NULL;
GO

-- 2. Stamp all pre-existing active records as Approved
--    (they already have a stock ledger row; treating them as Approved
--    keeps the UI consistent and prevents double-application on re-approve.)
UPDATE StockAdjustment
SET    [Status]      = 'Approved',
       [ApprovedBy]  = ISNULL([LastUpdatedBy], 'system'),
       [ApprovedDate]= [LASTUPDATE]
WHERE  [IsActive] = 1
  AND  [Status]   = 'Draft';   -- only newly added Draft rows (i.e. all of them at this point)
GO

-- 3. Add STOCK_ADJUSTMENT to ApprovalConfiguration if not already present
IF NOT EXISTS (
    SELECT 1 FROM ApprovalConfiguration WHERE DocumentType = 'STOCK_ADJUSTMENT'
)
BEGIN
    INSERT INTO ApprovalConfiguration
        (DocumentType, DisplayName, ApprovalRequired, ApproverRole, Remarks, IsActive, LASTUPDATE, LastUpdatedBy)
    VALUES
        ('STOCK_ADJUSTMENT', 'Stock Adjustment', 0, NULL,
         'Approval workflow for stock adjustments. Set ApprovalRequired=1 to enable.',
         1, GETDATE(), 'system');
END
GO

PRINT 'Migration 16 completed: StockAdjustment approval columns added, existing records stamped Approved.';
