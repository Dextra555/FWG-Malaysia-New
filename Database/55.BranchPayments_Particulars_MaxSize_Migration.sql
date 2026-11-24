-- ============================================================
-- Migration 55 : BranchPayments.Particulars column size increase
-- From  : varchar(255)
-- To    : NVARCHAR(MAX)
-- Reason: Payments page-ல user long text type பண்ணும்போது
--         255 chars truncate ஆகுது. MAX-க்கு மாத்துறோம்.
-- ============================================================

IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME   = 'BranchPayments'
      AND COLUMN_NAME  = 'Particulars'
      AND DATA_TYPE    = 'varchar'
)
BEGIN
    ALTER TABLE BranchPayments
        ALTER COLUMN Particulars NVARCHAR(MAX) NULL;

    PRINT 'BranchPayments.Particulars altered to NVARCHAR(MAX) successfully.';
END
ELSE
BEGIN
    PRINT 'BranchPayments.Particulars - already updated or column not found. Skipping.';
END
GO
