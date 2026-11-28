-- ============================================================
-- Migration 67: Update sp_Report_AssetMaster
--
-- Adds two result sets:
--   RS1 : Asset Master detail (existing, now includes payment term fields)
--   RS2 : Installment Schedule (AssetInstallment rows for this asset)
--
-- IDEMPOTENT: DROP + CREATE. Safe to re-run.
-- Target DB  : OBMSUATNEW
-- ============================================================

PRINT 'Migration 67: Updating sp_Report_AssetMaster (add installment schedule)...';

IF OBJECT_ID('dbo.sp_Report_AssetMaster', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.sp_Report_AssetMaster;
    PRINT 'sp_Report_AssetMaster: dropped existing.';
END
GO

CREATE PROCEDURE dbo.sp_Report_AssetMaster
    @AssetID INT
AS
BEGIN
    SET NOCOUNT ON;

    -- ── RS1: Asset Master detail ──────────────────────────────────────────
    SELECT
        am.ID,
        am.AssetCode,
        am.Name                                              AS AssetName,
        ISNULL(am.AssetType, '')                             AS AssetType,
        ISNULL(am.SerialNumber, '')                          AS SerialNumber,
        ISNULL(am.Description, '')                           AS Description,
        ISNULL(am.ModelNo, '')                               AS ModelNo,
        ISNULL(am.BrandName, '')                             AS BrandName,
        ISNULL(s.Name, '')                                   AS SupplierName,
        ISNULL(am.SupplierInvoiceNo, '')                     AS SupplierInvoiceNo,
        ISNULL(am.Branch, '')                                AS Branch,
        ISNULL(CAST(am.PurchaseAmount AS NVARCHAR), '')      AS PurchaseAmount,
        CONVERT(VARCHAR(20), am.PurchaseDate,       106)     AS PurchaseDate,
        CONVERT(VARCHAR(20), am.WarrantyExpiryDate, 106)     AS WarrantyExpiryDate,
        CONVERT(VARCHAR(20), am.RoadTaxRenewalDate, 106)     AS RoadTaxRenewalDate,
        CONVERT(VARCHAR(20), am.InsuranceRenewalDate,106)    AS InsuranceRenewalDate,
        am.AssetStatus,
        ISNULL(am.AssetCondition, '')                        AS AssetCondition,
        ISNULL(am.CurrentLocation, '')                       AS CurrentLocation,
        ISNULL(am.LocationStatus, '')                        AS LocationStatus,
        ISNULL(am.Remarks, '')                               AS Remarks,
        ISNULL(am.LastUpdatedBy, '')                         AS PreparedBy,
        ISNULL(am.PaymentTermType, 'FULL')                   AS PaymentTermType,
        ISNULL(am.DepositAmount, 0)                          AS DepositAmount,
        ISNULL(am.MonthlyInstallmentAmount, 0)               AS MonthlyInstallmentAmount,
        ISNULL(am.InstallmentMonths, 0)                      AS InstallmentMonths,
        CONVERT(VARCHAR(20), am.InstallmentStartDate, 106)   AS InstallmentStartDate,
        ISNULL(am.IsPreClosing, 0)                           AS IsPreClosing,
        ISNULL(am.PreClosingAmount, 0)                       AS PreClosingAmount,
        CONVERT(VARCHAR(20), am.PreClosingDate, 106)         AS PreClosingDate
    FROM  AssetMaster am
    LEFT  JOIN Suppliers s ON s.Id = am.SupplierID
    WHERE am.ID = @AssetID
      AND am.IsActive = 1;

    -- ── RS2: Installment Schedule ─────────────────────────────────────────
    SELECT
        ai.InstallmentNo                                     AS No,
        CONVERT(VARCHAR(20), ai.DueDate, 106)                AS DueDate,
        ai.Amount,
        ISNULL(ai.Status, 'Pending')                         AS Status,
        -- PaidAmount: if Status=Paid use full Amount, else 0
        CASE WHEN ISNULL(ai.Status,'Pending') = 'Paid'
             THEN ai.Amount ELSE 0
        END                                                  AS PaidAmount,
        -- BalanceAmount
        CASE WHEN ISNULL(ai.Status,'Pending') = 'Paid'
             THEN 0 ELSE ai.Amount
        END                                                  AS BalanceAmount
    FROM  AssetInstallment ai
    WHERE ai.AssetID  = @AssetID
      AND ai.IsActive = 1
    ORDER BY ai.InstallmentNo ASC;
END
GO

PRINT 'Migration 67: sp_Report_AssetMaster updated successfully.';
