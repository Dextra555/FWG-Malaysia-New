-- Migration 63: Add CreatedBy column to PurchaseRequisition table
-- Purpose: Track the original creator (drafter) of a PR separately from
--          LastUpdatedBy and SubmittedBy, so that the PR list correctly
--          shows the PR to the user who created it even when another user
--          (e.g. superadmin) submits or approves it.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PurchaseRequisition' AND COLUMN_NAME = 'CreatedBy'
)
BEGIN
    ALTER TABLE PurchaseRequisition
        ADD CreatedBy NVARCHAR(100) NULL;
END
GO

-- Backfill: for Draft PRs (not yet submitted) → LastUpdatedBy is the creator.
-- For Submitted/Approved/Rejected PRs → SubmittedBy is the submitter (may differ
-- from creator). We cannot recover the original creator for those rows, so we
-- leave CreatedBy NULL — it will be set correctly on future new PRs.
-- NOTE: If you know the original creator of existing rows, update them manually:
--   UPDATE PurchaseRequisition SET CreatedBy = 'valli' WHERE ID = 39;
UPDATE PurchaseRequisition
    SET CreatedBy = LastUpdatedBy
WHERE CreatedBy IS NULL
  AND Status = 'Draft';

PRINT 'Migration 63 complete: CreatedBy column ready in PurchaseRequisition.';
