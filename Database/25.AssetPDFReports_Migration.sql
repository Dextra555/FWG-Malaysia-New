-- ============================================================
-- OBMS Migration 25 — Asset Management PDF Report SPs
--
-- Creates 7 stored procedures for Asset PDF reports:
--   sp_Report_AssetMaster       — Asset Master detail card
--   sp_Report_AssetAllocation   — Asset Allocation record
--   sp_Report_AssetTransfer     — Asset Transfer record
--   sp_Report_AssetReturn       — Asset Return record
--   sp_Report_AssetMaintenance  — Asset Maintenance record
--   sp_Report_AssetDamage       — Asset Damage record
--   sp_Report_AssetDisposal     — Asset Disposal record
--
-- All SPs are IDEMPOTENT (DROP + CREATE). Safe to re-run.
-- All column names confirmed against:
--   Migration 06 (AssetManagement_Part6)
--   Migration 07 (AssetLocationTracking)
--   Migration 18 (AssetDisposal_ApprovalWorkflow)
-- ============================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '============================================================';
PRINT 'Migration 25 — Asset PDF Report SPs — Starting...';
PRINT '============================================================';


-- ─────────────────────────────────────────────────────────────
-- 1. sp_Report_AssetMaster
-- ─────────────────────────────────────────────────────────────
IF OBJECT_ID('sp_Report_AssetMaster', 'P') IS NOT NULL DROP PROCEDURE sp_Report_AssetMaster;
GO
CREATE PROCEDURE sp_Report_AssetMaster @AssetID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        am.ID,
        am.AssetCode,
        am.Name                                         AS AssetName,
        am.AssetType,
        ISNULL(am.SerialNumber, '')                     AS SerialNumber,
        ISNULL(am.Description, '')                      AS Description,
        ISNULL(am.ModelNo, '')                          AS ModelNo,
        ISNULL(am.BrandName, '')                        AS BrandName,
        ISNULL(s.Name, '')                              AS SupplierName,
        ISNULL(am.Branch, '')                           AS Branch,
        ISNULL(CAST(am.PurchaseAmount AS NVARCHAR), '') AS PurchaseAmount,
        CONVERT(VARCHAR(20), am.PurchaseDate,       106) AS PurchaseDate,
        CONVERT(VARCHAR(20), am.WarrantyExpiryDate, 106) AS WarrantyExpiryDate,
        am.AssetStatus,
        ISNULL(am.AssetCondition, '')                   AS AssetCondition,
        ISNULL(am.CurrentLocation, '')                  AS CurrentLocation,
        ISNULL(am.LocationStatus, '')                   AS LocationStatus,
        ISNULL(am.Remarks, '')                          AS Remarks,
        ISNULL(am.LastUpdatedBy, '')                    AS PreparedBy
    FROM  AssetMaster am
    LEFT  JOIN Suppliers s ON s.Id = am.SupplierID
    WHERE am.ID = @AssetID
      AND am.IsActive = 1;
END
GO
PRINT 'sp_Report_AssetMaster created.';


-- ─────────────────────────────────────────────────────────────
-- 2. sp_Report_AssetAllocation
-- ─────────────────────────────────────────────────────────────
IF OBJECT_ID('sp_Report_AssetAllocation', 'P') IS NOT NULL DROP PROCEDURE sp_Report_AssetAllocation;
GO
CREATE PROCEDURE sp_Report_AssetAllocation @AllocationID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        aa.ID,
        aa.AllocationNo,
        CONVERT(VARCHAR(20), aa.AllocationDate, 106)   AS AllocationDate,
        am.AssetCode,
        am.Name                                         AS AssetName,
        ISNULL(am.AssetType, '')                        AS AssetType,
        ISNULL(am.SerialNumber, '')                     AS SerialNumber,
        aa.AllocatedToType,
        ISNULL(aa.AllocatedToName, '')                  AS AllocatedToName,
        ISNULL(aa.BranchCode, '')                       AS BranchCode,
        ISNULL(aa.Remarks, '')                          AS Remarks,
        aa.IsActive,
        ISNULL(aa.LastUpdatedBy, '')                    AS PreparedBy
    FROM  AssetAllocation aa
    INNER JOIN AssetMaster am ON am.ID = aa.AssetID
    WHERE aa.ID = @AllocationID;
END
GO
PRINT 'sp_Report_AssetAllocation created.';


-- ─────────────────────────────────────────────────────────────
-- 3. sp_Report_AssetTransfer
-- ─────────────────────────────────────────────────────────────
IF OBJECT_ID('sp_Report_AssetTransfer', 'P') IS NOT NULL DROP PROCEDURE sp_Report_AssetTransfer;
GO
CREATE PROCEDURE sp_Report_AssetTransfer @TransferID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        at2.ID,
        at2.TransferNo,
        CONVERT(VARCHAR(20), at2.TransferDate, 106)    AS TransferDate,
        am.AssetCode,
        am.Name                                         AS AssetName,
        ISNULL(am.AssetType, '')                        AS AssetType,
        ISNULL(am.SerialNumber, '')                     AS SerialNumber,
        at2.FromType,
        ISNULL(at2.FromName, '')                        AS FromName,
        at2.ToType,
        ISNULL(at2.ToName, '')                          AS ToName,
        ISNULL(at2.TransferReason, '')                  AS TransferReason,
        ISNULL(at2.Remarks, '')                         AS Remarks,
        ISNULL(at2.LastUpdatedBy, '')                   AS PreparedBy
    FROM  AssetTransfer at2
    INNER JOIN AssetMaster am ON am.ID = at2.AssetID
    WHERE at2.ID = @TransferID
      AND at2.IsActive = 1;
END
GO
PRINT 'sp_Report_AssetTransfer created.';


-- ─────────────────────────────────────────────────────────────
-- 4. sp_Report_AssetReturn
-- ─────────────────────────────────────────────────────────────
IF OBJECT_ID('sp_Report_AssetReturn', 'P') IS NOT NULL DROP PROCEDURE sp_Report_AssetReturn;
GO
CREATE PROCEDURE sp_Report_AssetReturn @ReturnID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ar.ID,
        ar.ReturnNo,
        CONVERT(VARCHAR(20), ar.ReturnDate, 106)        AS ReturnDate,
        am.AssetCode,
        am.Name                                         AS AssetName,
        ISNULL(am.AssetType, '')                        AS AssetType,
        ISNULL(am.SerialNumber, '')                     AS SerialNumber,
        ISNULL(aa.AllocationNo, '')                     AS AllocationNo,
        ar.ReturnedByType,
        ISNULL(ar.ReturnedByName, '')                   AS ReturnedByName,
        ISNULL(ar.ReturnCondition, '')                  AS ReturnCondition,
        ISNULL(ar.ReceivedBy, '')                       AS ReceivedBy,
        ISNULL(ar.Remarks, '')                          AS Remarks,
        ISNULL(ar.LastUpdatedBy, '')                    AS PreparedBy
    FROM  AssetReturn ar
    INNER JOIN AssetMaster     am ON am.ID = ar.AssetID
    LEFT  JOIN AssetAllocation aa ON aa.ID = ar.AllocationID
    WHERE ar.ID = @ReturnID
      AND ar.IsActive = 1;
END
GO
PRINT 'sp_Report_AssetReturn created.';


-- ─────────────────────────────────────────────────────────────
-- 5. sp_Report_AssetMaintenance
-- ─────────────────────────────────────────────────────────────
IF OBJECT_ID('sp_Report_AssetMaintenance', 'P') IS NOT NULL DROP PROCEDURE sp_Report_AssetMaintenance;
GO
CREATE PROCEDURE sp_Report_AssetMaintenance @MaintenanceID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        amnt.ID,
        amnt.MaintenanceNo,
        CONVERT(VARCHAR(20), amnt.StartDate,        106) AS StartDate,
        CONVERT(VARCHAR(20), amnt.CompletionDate,   106) AS CompletionDate,
        am.AssetCode,
        am.Name                                          AS AssetName,
        ISNULL(am.AssetType, '')                         AS AssetType,
        ISNULL(am.SerialNumber, '')                      AS SerialNumber,
        amnt.MaintenanceType,
        ISNULL(amnt.ProblemDescription, '')              AS ProblemDescription,
        ISNULL(amnt.VendorName, '')                      AS VendorName,
        ISNULL(amnt.MaintenanceCost, 0)                  AS MaintenanceCost,
        amnt.Status,
        ISNULL(amnt.CompletionRemarks, '')               AS CompletionRemarks,
        ISNULL(amnt.Remarks, '')                         AS Remarks,
        ISNULL(amnt.LastUpdatedBy, '')                   AS PreparedBy
    FROM  AssetMaintenance amnt
    INNER JOIN AssetMaster am ON am.ID = amnt.AssetID
    WHERE amnt.ID = @MaintenanceID
      AND amnt.IsActive = 1;
END
GO
PRINT 'sp_Report_AssetMaintenance created.';


-- ─────────────────────────────────────────────────────────────
-- 6. sp_Report_AssetDamage
-- ─────────────────────────────────────────────────────────────
IF OBJECT_ID('sp_Report_AssetDamage', 'P') IS NOT NULL DROP PROCEDURE sp_Report_AssetDamage;
GO
CREATE PROCEDURE sp_Report_AssetDamage @DamageID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ad.ID,
        ad.DamageNo,
        CONVERT(VARCHAR(20), ad.DamageDate, 106)        AS DamageDate,
        am.AssetCode,
        am.Name                                         AS AssetName,
        ISNULL(am.AssetType, '')                        AS AssetType,
        ISNULL(am.SerialNumber, '')                     AS SerialNumber,
        ISNULL(ad.DamageType, '')                       AS DamageType,
        ad.DamageDescription,
        ISNULL(ad.DamageCondition, '')                  AS DamageCondition,
        ISNULL(ad.ReportedBy, '')                       AS ReportedBy,
        ISNULL(ad.EstimatedRepairCost, 0)               AS EstimatedRepairCost,
        ISNULL(ad.Remarks, '')                          AS Remarks,
        ISNULL(ad.LastUpdatedBy, '')                    AS PreparedBy
    FROM  AssetDamage ad
    INNER JOIN AssetMaster am ON am.ID = ad.AssetID
    WHERE ad.ID = @DamageID
      AND ad.IsActive = 1;
END
GO
PRINT 'sp_Report_AssetDamage created.';


-- ─────────────────────────────────────────────────────────────
-- 7. sp_Report_AssetDisposal
-- ─────────────────────────────────────────────────────────────
IF OBJECT_ID('sp_Report_AssetDisposal', 'P') IS NOT NULL DROP PROCEDURE sp_Report_AssetDisposal;
GO
CREATE PROCEDURE sp_Report_AssetDisposal @DisposalID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        adi.ID,
        adi.DisposalNo,
        CONVERT(VARCHAR(20), adi.DisposalDate,  106)    AS DisposalDate,
        am.AssetCode,
        am.Name                                         AS AssetName,
        ISNULL(am.AssetType, '')                        AS AssetType,
        ISNULL(am.SerialNumber, '')                     AS SerialNumber,
        adi.DisposalType,
        adi.DisposalReason,
        ISNULL(adi.DisposalValue, 0)                    AS DisposalValue,
        ISNULL(adi.ApprovedBy, '')                      AS ApprovedBy,
        CONVERT(VARCHAR(20), adi.ApprovedDate,   106)   AS ApprovedDate,
        adi.Status,
        ISNULL(adi.SubmittedBy, '')                     AS SubmittedBy,
        ISNULL(adi.ApprovalRemarks, '')                 AS ApprovalRemarks,
        ISNULL(adi.Remarks, '')                         AS Remarks,
        ISNULL(adi.LastUpdatedBy, '')                   AS PreparedBy
    FROM  AssetDisposal adi
    INNER JOIN AssetMaster am ON am.ID = adi.AssetID
    WHERE adi.ID = @DisposalID
      AND adi.IsActive = 1;
END
GO
PRINT 'sp_Report_AssetDisposal created.';

PRINT '============================================================';
PRINT 'Migration 25 complete — all 7 asset report SPs created.';
PRINT '============================================================';
