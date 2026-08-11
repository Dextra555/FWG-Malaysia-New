-- Migration: Add/Fix SKBBK column in EmployeeSalaryDetails table
-- Date: 2026-07-16
-- Description: SKBBK as BIT (boolean) with default 1 (Yes)

-- Step 1: If column exists as wrong type (NVARCHAR), drop and recreate
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'EmployeeSalaryDetails' 
    AND COLUMN_NAME = 'SKBBK'
    AND DATA_TYPE != 'bit'
)
BEGIN
    ALTER TABLE EmployeeSalaryDetails DROP COLUMN SKBBK;
    PRINT 'Dropped old SKBBK column (wrong type).';
END

-- Step 2: Add column as BIT if not exists
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'EmployeeSalaryDetails' 
    AND COLUMN_NAME = 'SKBBK'
)
BEGIN
    ALTER TABLE EmployeeSalaryDetails
    ADD SKBBK BIT NOT NULL DEFAULT 1;

    PRINT 'Column SKBBK added as BIT with default 1 (Yes).';
END
ELSE
BEGIN
    PRINT 'Column SKBBK already exists with correct type.';
END
