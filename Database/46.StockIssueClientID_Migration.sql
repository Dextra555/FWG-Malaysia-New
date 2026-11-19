-- ============================================================
-- Migration 46: Add ClientID column to StockIssueDocDetail
-- Allows tracking which client each issued item belongs to.
-- Run once on OBMSUATNEW (and FWGServerDB for live).
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME  = 'StockIssueDocDetail'
      AND COLUMN_NAME = 'ClientID'
)
BEGIN
    ALTER TABLE StockIssueDocDetail
        ADD ClientID INT NULL;

    PRINT 'Column ClientID added to StockIssueDocDetail.';
END
ELSE
BEGIN
    PRINT 'Column ClientID already exists in StockIssueDocDetail — skipped.';
END
