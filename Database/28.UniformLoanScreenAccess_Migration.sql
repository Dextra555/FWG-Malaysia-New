-- ============================================================
-- Migration 28 : Register User Access screens for Payroll
--                Employee Uniform Loan pages
-- Screens added:
--   • Employee Uniform          (new-employee-uniform-loan,
--                                search-employee-uniform-loan)
--   • Uniform Loan Stock Issues (uniform-loan-stock-link)
-- ============================================================

-- ── Employee Uniform ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Employee Uniform')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Employee Uniform', 'Payroll', GETDATE(), 'system');
    PRINT 'ScreenList: Employee Uniform added.';
END
ELSE PRINT 'ScreenList: Employee Uniform already exists. Skipping.';

-- ── Uniform Loan Stock Issues ────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM ScreenList WHERE ScreenName = 'Uniform Loan Stock Issues')
BEGIN
    INSERT INTO ScreenList (ScreenName, Category, LastUpdate, LastUpdatedBy)
    VALUES ('Uniform Loan Stock Issues', 'Payroll', GETDATE(), 'system');
    PRINT 'ScreenList: Uniform Loan Stock Issues added.';
END
ELSE PRINT 'ScreenList: Uniform Loan Stock Issues already exists. Skipping.';

-- ── Grant superadmin full access to both screens ─────────────
IF NOT EXISTS (SELECT 1 FROM OBMSPermissions
               WHERE Name = 'superadmin' AND ScreenName = 'Employee Uniform')
BEGIN
    INSERT INTO OBMSPermissions
        (Name, ScreenName, [Create], [Read], [Update], [Delete], LastUpdatedBy)
    VALUES
        ('superadmin', 'Employee Uniform', 1, 1, 1, 1, 'system');
    PRINT 'OBMSPermissions: superadmin → Employee Uniform added.';
END
ELSE PRINT 'OBMSPermissions: superadmin → Employee Uniform already exists. Skipping.';

IF NOT EXISTS (SELECT 1 FROM OBMSPermissions
               WHERE Name = 'superadmin' AND ScreenName = 'Uniform Loan Stock Issues')
BEGIN
    INSERT INTO OBMSPermissions
        (Name, ScreenName, [Create], [Read], [Update], [Delete], LastUpdatedBy)
    VALUES
        ('superadmin', 'Uniform Loan Stock Issues', 1, 1, 1, 1, 'system');
    PRINT 'OBMSPermissions: superadmin → Uniform Loan Stock Issues added.';
END
ELSE PRINT 'OBMSPermissions: superadmin → Uniform Loan Stock Issues already exists. Skipping.';

-- ── Verify ──────────────────────────────────────────────────
SELECT ScreenName, Category FROM ScreenList
WHERE ScreenName IN ('Employee Uniform', 'Uniform Loan Stock Issues')
ORDER BY ScreenName;
