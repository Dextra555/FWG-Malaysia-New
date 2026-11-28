-- ============================================================
-- Migration 66: Update sp_BranchAssetReport
--
-- Adds "Asset Maintenance" as a special AssetType filter.
-- When @AssetType = 'Asset Maintenance', returns AssetMaintenance
-- records (Scheduled / In_Progress) instead of AssetAllocation data.
--
-- Columns (Maintenance mode):
--   BranchName, AssetCode, AssetName, MaintenanceNo, MaintenanceType,
--   StartDate, CompletionDate, Status, VendorName, MaintenanceCost,
--   ProblemDescription, Remarks
--
-- IDEMPOTENT: DROP + CREATE. Safe to re-run.
-- Target DB  : OBMSUATNEW
-- ============================================================

PRINT 'Migration 66: Updating sp_BranchAssetReport (add Asset Maintenance type)...';

IF OBJECT_ID('dbo.sp_BranchAssetReport', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_BranchAssetReport;
    PRINT 'sp_BranchAssetReport: dropped existing.';
END
GO

CREATE PROCEDURE dbo.sp_BranchAssetReport
    @BranchID    INT           = 0,
    @AssetStatus NVARCHAR(30)  = 'ALL',
    @AssetType   NVARCHAR(50)  = 'ALL'
AS
BEGIN
    SET NOCOUNT ON;

    -- ════════════════════════════════════════════════════════════════════════
    -- SPECIAL MODE: Asset Maintenance
    -- Returns AssetMaintenance records (not AssetAllocation)
    -- ════════════════════════════════════════════════════════════════════════
    IF @AssetType = 'Asset Maintenance'
    BEGIN
        SELECT
            ISNULL(bm.Name, ISNULL(mnt.BranchName, 'Unallocated'))  AS BranchName,
            am.AssetCode,
            am.Name                                          AS AssetName,
            ISNULL(am.AssetType, '')                         AS AssetType,
            mnt.MaintenanceNo,
            mnt.MaintenanceType,
            CONVERT(VARCHAR(20), mnt.StartDate, 106)         AS StartDate,
            CASE WHEN mnt.CompletionDate IS NOT NULL
                 THEN CONVERT(VARCHAR(20), mnt.CompletionDate, 106)
                 ELSE ''
            END                                              AS CompletionDate,
            mnt.Status                                       AS MaintenanceStatus,
            ISNULL(mnt.VendorName, '')                       AS VendorName,
            ISNULL(mnt.MaintenanceCost, 0)                   AS MaintenanceCost,
            ISNULL(mnt.ProblemDescription, '')               AS ProblemDescription,
            ISNULL(mnt.Remarks, '')                          AS Remarks,
            ISNULL(mnt.LastUpdatedBy, '')                    AS LastUpdatedBy
        FROM  AssetMaintenance mnt
        INNER JOIN AssetMaster am ON am.ID = mnt.AssetID
        -- Get branch from AssetAllocation (most recent active allocation)
        LEFT JOIN (
            SELECT aa2.AssetID, bm2.Name AS BranchName,
                   ROW_NUMBER() OVER (PARTITION BY aa2.AssetID ORDER BY aa2.ID DESC) AS rn
            FROM AssetAllocation aa2
            INNER JOIN BranchMaster bm2 ON bm2.Code = aa2.BranchCode
            WHERE aa2.IsActive = 1
              AND aa2.AllocatedToType = 'Branch'
        ) alloc ON alloc.AssetID = am.ID AND alloc.rn = 1
        LEFT JOIN BranchMaster bm ON bm.Name = alloc.BranchName
        WHERE mnt.IsActive = 1
          AND am.IsActive  = 1
          AND (@AssetStatus = 'ALL' OR mnt.Status = @AssetStatus)
          AND (@BranchID   = 0 OR bm.ID = @BranchID)
        ORDER BY
            ISNULL(bm.Name, ISNULL(mnt.BranchName, 'Unallocated')),
            am.Name,
            mnt.StartDate DESC;
        RETURN;
    END

    -- ════════════════════════════════════════════════════════════════════════
    -- SPECIAL MODE: Asset Due List
    -- Returns Road Tax, Insurance, Service due dates from AssetMaster
    -- ════════════════════════════════════════════════════════════════════════
    IF @AssetType = 'Asset Due List'
    BEGIN
        SELECT
            ISNULL(alloc.BranchName, 'Unallocated')          AS BranchName,
            am.AssetCode,
            am.Name                                           AS AssetName,
            ISNULL(am.AssetType, '')                          AS AssetType,
            am.AssetStatus,
            ISNULL(am.Branch, '')                             AS Branch,
            -- Road Tax
            CASE WHEN am.RoadTaxRenewalDate IS NOT NULL
                 THEN CONVERT(VARCHAR(20), am.RoadTaxRenewalDate, 106)
                 ELSE '' END                                  AS RoadTaxRenewalDate,
            CASE WHEN am.RoadTaxRenewalDate IS NOT NULL
                 THEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(am.RoadTaxRenewalDate AS DATE))
                 ELSE NULL END                                AS RoadTaxDaysLeft,
            -- Insurance
            CASE WHEN am.InsuranceRenewalDate IS NOT NULL
                 THEN CONVERT(VARCHAR(20), am.InsuranceRenewalDate, 106)
                 ELSE '' END                                  AS InsuranceRenewalDate,
            CASE WHEN am.InsuranceRenewalDate IS NOT NULL
                 THEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(am.InsuranceRenewalDate AS DATE))
                 ELSE NULL END                                AS InsuranceDaysLeft,
            -- Service (next due = ServiceLastDate + 3 months)
            CASE WHEN am.ServiceLastDate IS NOT NULL
                 THEN CONVERT(VARCHAR(20), DATEADD(MONTH, 3, am.ServiceLastDate), 106)
                 ELSE '' END                                  AS ServiceDueDate,
            CASE WHEN am.ServiceLastDate IS NOT NULL
                 THEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(DATEADD(MONTH, 3, am.ServiceLastDate) AS DATE))
                 ELSE NULL END                                AS ServiceDaysLeft,
            -- Warranty
            CASE WHEN am.WarrantyExpiryDate IS NOT NULL
                 THEN CONVERT(VARCHAR(20), am.WarrantyExpiryDate, 106)
                 ELSE '' END                                  AS WarrantyExpiryDate,
            CASE WHEN am.WarrantyExpiryDate IS NOT NULL
                 THEN DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(am.WarrantyExpiryDate AS DATE))
                 ELSE NULL END                                AS WarrantyDaysLeft
        FROM AssetMaster am
        LEFT JOIN (
            SELECT aa2.AssetID, bm2.Name AS BranchName,
                   ROW_NUMBER() OVER (PARTITION BY aa2.AssetID ORDER BY aa2.ID DESC) AS rn
            FROM AssetAllocation aa2
            INNER JOIN BranchMaster bm2 ON bm2.Code = aa2.BranchCode
            WHERE aa2.IsActive = 1 AND aa2.AllocatedToType = 'Branch'
        ) alloc ON alloc.AssetID = am.ID AND alloc.rn = 1
        LEFT JOIN BranchMaster bm ON bm.Name = alloc.BranchName
        WHERE am.IsActive = 1
          AND (@BranchID = 0 OR bm.ID = @BranchID)
          AND (
                am.RoadTaxRenewalDate    IS NOT NULL OR
                am.InsuranceRenewalDate  IS NOT NULL OR
                am.ServiceLastDate       IS NOT NULL OR
                am.WarrantyExpiryDate    IS NOT NULL
              )
        ORDER BY
            ISNULL(alloc.BranchName, 'Unallocated'),
            am.Name;
        RETURN;
    END

    -- ════════════════════════════════════════════════════════════════════════
    -- NORMAL MODE: Asset Allocation (existing logic unchanged)
    -- ════════════════════════════════════════════════════════════════════════

    -- Section 1: Allocated assets (AllocatedToType = 'Branch')
    SELECT
        ISNULL(bm.Name, aa.AllocatedToName)   AS BranchName,
        ISNULL(bm.Code, aa.BranchCode)        AS BranchCode,
        aa.AllocationNo,
        CONVERT(VARCHAR(20), aa.AllocationDate, 106) AS AllocationDate,
        am.AssetCode,
        am.Name                               AS AssetName,
        ISNULL(am.AssetType, '')              AS AssetType,
        ISNULL(am.SerialNumber, '')           AS SerialNumber,
        ISNULL(am.BrandName, '')              AS BrandName,
        ISNULL(am.ModelNo, '')                AS ModelNo,
        CASE WHEN am.PurchaseDate IS NOT NULL
             THEN CONVERT(VARCHAR(20), am.PurchaseDate, 106)
             ELSE ''
        END                                   AS PurchaseDate,
        ISNULL(am.PurchaseAmount, 0)          AS PurchaseValue,
        am.AssetStatus,
        ISNULL(am.AssetCondition, '')         AS AssetCondition,
        ISNULL(am.CurrentLocation, '')        AS CurrentLocation,
        ISNULL(am.Remarks, '')                AS AssetRemarks,
        ISNULL(aa.Remarks, '')                AS AllocationRemarks,
        ISNULL(aa.LastUpdatedBy, '')          AS AllocatedBy
    FROM AssetAllocation aa
        INNER JOIN AssetMaster am ON am.ID = aa.AssetID
        LEFT  JOIN BranchMaster bm ON bm.Code = aa.BranchCode
    WHERE aa.IsActive = 1
      AND am.IsActive = 1
      AND aa.AllocatedToType = 'Branch'
      AND (@BranchID    = 0 OR bm.ID = @BranchID)
      AND (@AssetStatus = 'ALL' OR am.AssetStatus = @AssetStatus)
      AND (@AssetType   = 'ALL' OR am.AssetType   = @AssetType)

    UNION ALL

    -- Section 2: Unallocated assets
    SELECT
        'Unallocated'                          AS BranchName,
        ''                                     AS BranchCode,
        ''                                     AS AllocationNo,
        ''                                     AS AllocationDate,
        am.AssetCode,
        am.Name                                AS AssetName,
        ISNULL(am.AssetType, '')               AS AssetType,
        ISNULL(am.SerialNumber, '')            AS SerialNumber,
        ISNULL(am.BrandName, '')               AS BrandName,
        ISNULL(am.ModelNo, '')                 AS ModelNo,
        CASE WHEN am.PurchaseDate IS NOT NULL
             THEN CONVERT(VARCHAR(20), am.PurchaseDate, 106)
             ELSE ''
        END                                    AS PurchaseDate,
        ISNULL(am.PurchaseAmount, 0)           AS PurchaseValue,
        am.AssetStatus,
        ISNULL(am.AssetCondition, '')          AS AssetCondition,
        ISNULL(am.CurrentLocation, '')         AS CurrentLocation,
        ISNULL(am.Remarks, '')                 AS AssetRemarks,
        ''                                     AS AllocationRemarks,
        ISNULL(am.LastUpdatedBy, '')           AS AllocatedBy
    FROM AssetMaster am
    WHERE am.IsActive = 1
      AND NOT EXISTS (
            SELECT 1 FROM AssetAllocation aa2
            WHERE aa2.AssetID = am.ID
              AND aa2.IsActive = 1
              AND aa2.AllocatedToType = 'Branch'
          )
      AND @BranchID   = 0
      AND (@AssetStatus = 'ALL' OR am.AssetStatus = @AssetStatus)
      AND (@AssetType   = 'ALL' OR am.AssetType   = @AssetType)

    ORDER BY
        BranchName    ASC,
        AssetType     ASC,
        AssetName     ASC;
END
GO

PRINT 'Migration 66: sp_BranchAssetReport updated successfully.';
