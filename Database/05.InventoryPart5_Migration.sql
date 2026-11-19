-- ============================================================
-- OBMS Inventory Part 5 Migration
-- Employee/Guard Item Tracking, Stock Balance Summary,
-- Employee Issue Register Report
--
-- No new tables required — all data is derived from
-- Part 4 tables: StockIssueDoc, StockIssueDocDetail,
-- StockIssueReturn, StockIssueReturnDetail, StockLedger.
--
-- This script is IDEMPOTENT — safe to re-run.
-- Target database : OBMSUATNEW (or live equivalent)
-- Part            : 5 – Employee/Guard Tracking + Reports
-- ============================================================

PRINT '============================================================';
PRINT 'OBMS Part 5 Inventory Migration - Starting...';
PRINT '============================================================';

-- ── ScreenList entries ─────────────────────────────────────────────────────

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Employee Item Holding')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Employee Item Holding', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Employee Item Holding added.';
END
ELSE PRINT 'ScreenList: Employee Item Holding already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Stock Balance Summary')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Stock Balance Summary', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Stock Balance Summary added.';
END
ELSE PRINT 'ScreenList: Stock Balance Summary already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Employee Issue Register')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Employee Issue Register', 'Inventory', GETDATE(), 'MIGRATION');
    PRINT 'ScreenList: Employee Issue Register added.';
END
ELSE PRINT 'ScreenList: Employee Issue Register already exists. Skipping.';

PRINT '============================================================';
PRINT 'OBMS Part 5 Inventory Migration - Completed.';
PRINT '============================================================';
