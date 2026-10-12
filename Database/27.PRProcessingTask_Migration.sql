-- ═══════════════════════════════════════════════════════════════════════════
-- Migration 27 : PRProcessingTask
--
-- Purpose : Tracks the Purchase department's work queue for approved PRs.
--           Each approved PR gets one task row (UNIQUE constraint on PRID).
--           A Purchase user "claims" a task, becoming the sole owner.
--           The task is marked Completed when a PO is successfully created.
--
-- Status values : Pending | Claimed | Completed
--
-- Safe to run multiple times: every block is guarded by IF NOT EXISTS.
-- ═══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE  TABLE_NAME = 'PRProcessingTask'
)
BEGIN
    CREATE TABLE PRProcessingTask (
        ID            INT IDENTITY(1,1) PRIMARY KEY,
        PRID          INT           NOT NULL,           -- FK → PurchaseRequisition.ID
        PRNumber      NVARCHAR(50)  NOT NULL,           -- denormalised — avoids join on every inbox query
        ClaimedBy     NVARCHAR(100) NULL,               -- NULL = unclaimed
        ClaimedDate   DATETIME      NULL,
        Status        NVARCHAR(50)  NOT NULL
                      CONSTRAINT DF_PRProcessingTask_Status DEFAULT 'Pending',
        CompletedBy   NVARCHAR(100) NULL,               -- who created the PO
        CompletedDate DATETIME      NULL,
        PONumber      NVARCHAR(50)  NULL,               -- the PO that fulfilled this task
        CreatedDate   DATETIME      NOT NULL
                      CONSTRAINT DF_PRProcessingTask_CreatedDate DEFAULT GETDATE(),
        CreatedBy     NVARCHAR(100) NULL,
        LASTUPDATE    DATETIME      NOT NULL
                      CONSTRAINT DF_PRProcessingTask_LASTUPDATE  DEFAULT GETDATE(),
        LastUpdatedBy NVARCHAR(100) NULL,

        -- One task per PR, enforced at DB level so concurrent claim attempts
        -- always have exactly one winner (combined with the conditional UPDATE).
        CONSTRAINT UQ_PRProcessingTask_PRID UNIQUE (PRID)
    );
    PRINT 'PRProcessingTask: table created.';
END
ELSE
    PRINT 'PRProcessingTask: already exists — skipped.';
GO

-- Index: fast lookup by Status + ClaimedBy (inbox query pattern)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE  name      = 'IX_PRProcessingTask_Status'
      AND  object_id = OBJECT_ID('PRProcessingTask')
)
BEGIN
    CREATE INDEX IX_PRProcessingTask_Status
        ON PRProcessingTask (Status, ClaimedBy);
    PRINT 'PRProcessingTask: IX_PRProcessingTask_Status created.';
END
ELSE
    PRINT 'PRProcessingTask: IX_PRProcessingTask_Status already exists.';
GO
