-- Migration: Add SplitSalaryPayment column to PaySlip and PaySlipAudit tables
-- Date: 2026-11-14
-- Description: Store the SplitSalaryPayment flag from EmployeeSalaryDetails into PaySlip
--              during salary processing, so reports can reference it per period.

-- PaySlip table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'PaySlip' AND COLUMN_NAME = 'SplitSalaryPayment'
)
BEGIN
    ALTER TABLE PaySlip ADD SplitSalaryPayment BIT NOT NULL DEFAULT 0;
    PRINT 'SplitSalaryPayment added to PaySlip.';
END
ELSE
    PRINT 'SplitSalaryPayment already exists in PaySlip.';

-- PaySlipAudit table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'PaySlipAudit' AND COLUMN_NAME = 'SplitSalaryPayment'
)
BEGIN
    ALTER TABLE PaySlipAudit ADD SplitSalaryPayment BIT NOT NULL DEFAULT 0;
    PRINT 'SplitSalaryPayment added to PaySlipAudit.';
END
ELSE
    PRINT 'SplitSalaryPayment already exists in PaySlipAudit.';
