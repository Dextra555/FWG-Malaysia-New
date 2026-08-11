-- Migration: Add SKBBKContribution column to PaySlip and PaySlipAudit tables
-- Date: 2026-07-16
-- Description: SKBBK amount calculated from SOCSO.SocsoSKBBK rate, saved per employee per period

-- PaySlip table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'PaySlip' AND COLUMN_NAME = 'SKBBKContribution'
)
BEGIN
    ALTER TABLE PaySlip ADD SKBBKContribution DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT 'SKBBKContribution added to PaySlip.';
END
ELSE
    PRINT 'SKBBKContribution already exists in PaySlip.';

-- PaySlipAudit table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'PaySlipAudit' AND COLUMN_NAME = 'SKBBKContribution'
)
BEGIN
    ALTER TABLE PaySlipAudit ADD SKBBKContribution DECIMAL(18,2) NOT NULL DEFAULT 0;
    PRINT 'SKBBKContribution added to PaySlipAudit.';
END
ELSE
    PRINT 'SKBBKContribution already exists in PaySlipAudit.';
