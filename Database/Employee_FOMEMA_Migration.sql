-- Migration: Add FOMEMA columns to Employee and EmployeeHistory tables
-- Date: 2026-07-02

-- Add FOMEMA columns to Employee table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Employee' AND COLUMN_NAME = 'FOMEMA_MedicalCheckupDate'
)
BEGIN
    ALTER TABLE Employee
    ADD FOMEMA_MedicalCheckupDate DATETIME NULL;
    PRINT 'Column FOMEMA_MedicalCheckupDate added to Employee table.';
END
ELSE
BEGIN
    PRINT 'Column FOMEMA_MedicalCheckupDate already exists in Employee table. Skipping.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Employee' AND COLUMN_NAME = 'FOMEMA_1stAppealDate'
)
BEGIN
    ALTER TABLE Employee
    ADD FOMEMA_1stAppealDate DATETIME NULL;
    PRINT 'Column FOMEMA_1stAppealDate added to Employee table.';
END
ELSE
BEGIN
    PRINT 'Column FOMEMA_1stAppealDate already exists in Employee table. Skipping.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Employee' AND COLUMN_NAME = 'FOMEMA_2ndAppealDate'
)
BEGIN
    ALTER TABLE Employee
    ADD FOMEMA_2ndAppealDate DATETIME NULL;
    PRINT 'Column FOMEMA_2ndAppealDate added to Employee table.';
END
ELSE
BEGIN
    PRINT 'Column FOMEMA_2ndAppealDate already exists in Employee table. Skipping.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Employee' AND COLUMN_NAME = 'FOMEMA_NCDMonitoringDate'
)
BEGIN
    ALTER TABLE Employee
    ADD FOMEMA_NCDMonitoringDate DATETIME NULL;
    PRINT 'Column FOMEMA_NCDMonitoringDate added to Employee table.';
END
ELSE
BEGIN
    PRINT 'Column FOMEMA_NCDMonitoringDate already exists in Employee table. Skipping.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Employee' AND COLUMN_NAME = 'FOMEMA_ResultDate'
)
BEGIN
    ALTER TABLE Employee
    ADD FOMEMA_ResultDate DATETIME NULL;
    PRINT 'Column FOMEMA_ResultDate added to Employee table.';
END
ELSE
BEGIN
    PRINT 'Column FOMEMA_ResultDate already exists in Employee table. Skipping.';
END

-- Add FOMEMA columns to EmployeeHistory table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'EmployeeHistory' AND COLUMN_NAME = 'FOMEMA_MedicalCheckupDate'
)
BEGIN
    ALTER TABLE EmployeeHistory
    ADD FOMEMA_MedicalCheckupDate DATETIME NULL;
    PRINT 'Column FOMEMA_MedicalCheckupDate added to EmployeeHistory table.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'EmployeeHistory' AND COLUMN_NAME = 'FOMEMA_1stAppealDate'
)
BEGIN
    ALTER TABLE EmployeeHistory
    ADD FOMEMA_1stAppealDate DATETIME NULL;
    PRINT 'Column FOMEMA_1stAppealDate added to EmployeeHistory table.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'EmployeeHistory' AND COLUMN_NAME = 'FOMEMA_2ndAppealDate'
)
BEGIN
    ALTER TABLE EmployeeHistory
    ADD FOMEMA_2ndAppealDate DATETIME NULL;
    PRINT 'Column FOMEMA_2ndAppealDate added to EmployeeHistory table.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'EmployeeHistory' AND COLUMN_NAME = 'FOMEMA_NCDMonitoringDate'
)
BEGIN
    ALTER TABLE EmployeeHistory
    ADD FOMEMA_NCDMonitoringDate DATETIME NULL;
    PRINT 'Column FOMEMA_NCDMonitoringDate added to EmployeeHistory table.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'EmployeeHistory' AND COLUMN_NAME = 'FOMEMA_ResultDate'
)
BEGIN
    ALTER TABLE EmployeeHistory
    ADD FOMEMA_ResultDate DATETIME NULL;
    PRINT 'Column FOMEMA_ResultDate added to EmployeeHistory table.';
END

-- Also add missing VisaExpiryDate and PassportExpiryDate to EmployeeHistory if not present
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'EmployeeHistory' AND COLUMN_NAME = 'VisaExpiryDate'
)
BEGIN
    ALTER TABLE EmployeeHistory
    ADD VisaExpiryDate DATETIME NULL;
    PRINT 'Column VisaExpiryDate added to EmployeeHistory table.';
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'EmployeeHistory' AND COLUMN_NAME = 'PassportExpiryDate'
)
BEGIN
    ALTER TABLE EmployeeHistory
    ADD PassportExpiryDate DATETIME NULL;
    PRINT 'Column PassportExpiryDate added to EmployeeHistory table.';
END
