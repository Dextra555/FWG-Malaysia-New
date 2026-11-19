-- ═══════════════════════════════════════════════════════════════════════════
-- Migration 26 : Department column on OBMSUsers
--
-- Purpose : Supports department-based notification routing.
--           When a Purchase Requisition is approved, all active users
--           whose Department = 'Purchase' are notified in addition to
--           the PR submitter.
--
-- Safe to run multiple times: guarded by IF NOT EXISTS.
-- ═══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1
    FROM   INFORMATION_SCHEMA.COLUMNS
    WHERE  TABLE_NAME  = 'OBMSUsers'
      AND  COLUMN_NAME = 'Department'
)
BEGIN
    ALTER TABLE OBMSUsers
        ADD Department NVARCHAR(100) NULL;
    PRINT 'OBMSUsers: Department column added.';
END
ELSE
    PRINT 'OBMSUsers: Department column already exists — skipped.';
GO
