-- Migration: Add VisaExpiryDate and PassportExpiryDate columns to Employee table
-- Date: 2026-06-25

-- Add VisaExpiryDate column (nullable DateTime)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Employee' AND COLUMN_NAME = 'VisaExpiryDate'
)
BEGIN
    ALTER TABLE Employee
    ADD VisaExpiryDate DATETIME NULL;
    PRINT 'Column VisaExpiryDate added to Employee table.';
END
ELSE
BEGIN
    PRINT 'Column VisaExpiryDate already exists in Employee table. Skipping.';
END

-- Add PassportExpiryDate column (nullable DateTime)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Employee' AND COLUMN_NAME = 'PassportExpiryDate'
)
BEGIN
    ALTER TABLE Employee
    ADD PassportExpiryDate DATETIME NULL;
    PRINT 'Column PassportExpiryDate added to Employee table.';
END
ELSE
BEGIN
    PRINT 'Column PassportExpiryDate already exists in Employee table. Skipping.';
END
