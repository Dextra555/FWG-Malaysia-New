-- ============================================================
-- Migration 18: Asset Disposal Approval Workflow
-- Adds Status, SubmittedBy, SubmittedDate, ApprovalRemarks
-- to AssetDisposal table. (ApprovedBy + ApprovedDate already exist.)
--
-- IMPORTANT BEHAVIOUR CHANGE:
--   Before: SaveAssetDisposal immediately sets Asset.AssetStatus = 'Disposed'.
--   After:  SaveAssetDisposal saves as Draft (asset unchanged).
--           ApproveAD sets Asset.AssetStatus = 'Disposed'.
--
-- Existing records are stamped Status = 'Approved' and
-- ApprovedDate is backfilled from LASTUPDATE so asset
-- statuses already set to 'Disposed' remain consistent.
-- ============================================================

-- 1. Add new columns
ALTER TABLE AssetDisposal ADD
    [Status]          NVARCHAR(20)   NOT NULL DEFAULT 'Draft',
    [SubmittedBy]     NVARCHAR(100)  NULL,
    [SubmittedDate]   DATETIME       NULL,
    [ApprovedDate]    DATETIME       NULL,
    [ApprovalRemarks] NVARCHAR(500)  NULL;
GO

-- 2. Stamp all pre-existing active records as Approved
--    (they already disposed the asset at save time)
UPDATE AssetDisposal
SET    [Status]      = 'Approved',
       [ApprovedDate]= [LASTUPDATE]
WHERE  [IsActive] = 1
  AND  [Status]   = 'Draft';
GO

-- 3. Add ASSET_DISPOSAL to ApprovalConfiguration if missing
IF NOT EXISTS (
    SELECT 1 FROM ApprovalConfiguration WHERE DocumentType = 'ASSET_DISPOSAL'
)
BEGIN
    INSERT INTO ApprovalConfiguration
        (DocumentType, DisplayName, ApprovalRequired, ApproverRole, Remarks, IsActive, LASTUPDATE, LastUpdatedBy)
    VALUES
        ('ASSET_DISPOSAL', 'Asset Disposal', 0, NULL,
         'Approval workflow for asset disposals. Set ApprovalRequired=1 to enable.',
         1, GETDATE(), 'system');
END
GO

PRINT 'Migration 18 completed: AssetDisposal approval columns added, existing records stamped Approved.';
