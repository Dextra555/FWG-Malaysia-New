-- ============================================================
-- Migration 31 — StockIssueDoc: IsDeduction flag
--
-- Purpose : When IssueToType = Guard or Staff, the user can
--           mark the stock issue as a salary Deduction or
--           Non-Deduction. Only issues marked IsDeduction = 1
--           will appear in the Uniform Loan — Pending Stock
--           Issues page.
-- Created : 2026-11-03
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'StockIssueDoc'
      AND COLUMN_NAME = 'IsDeduction'
)
BEGIN
    ALTER TABLE [dbo].[StockIssueDoc]
        ADD [IsDeduction] BIT NOT NULL DEFAULT 0;

    PRINT 'StockIssueDoc.IsDeduction column added (default 0 = Non-Deduction).';
END
ELSE
    PRINT 'StockIssueDoc.IsDeduction already exists — skipped.';

PRINT 'Migration 31 complete.';
