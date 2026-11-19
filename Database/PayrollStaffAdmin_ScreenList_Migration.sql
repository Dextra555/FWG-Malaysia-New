-- ============================================================
-- Migration: Add 'Payroll Staff Admin' screen to ScreenList
-- Category : Payroll
-- Purpose  : Controls visibility of Staff employee type in
--            Payroll module (mirrors 'Employee Master Staff Admin'
--            in the Master module)
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Payroll Staff Admin')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Payroll Staff Admin', 'Payroll', GETDATE(), 'MIGRATION');
    PRINT 'Payroll Staff Admin added to ScreenList.';
END
ELSE
BEGIN
    PRINT 'Payroll Staff Admin already exists in ScreenList — skipped.';
END
