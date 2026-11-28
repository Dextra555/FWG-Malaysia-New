-- ============================================================
-- Migration 61: Asset Maintenance Payment Support
-- Adds MaintenanceID to BranchPaymentDetails
-- Adds PaymentID to AssetMaintenance
-- ============================================================

-- 1. BranchPaymentDetails: add MaintenanceID (nullable int)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'BranchPaymentDetails' AND COLUMN_NAME = 'MaintenanceID'
)
BEGIN
    ALTER TABLE BranchPaymentDetails
    ADD MaintenanceID INT NULL;
END

-- 2. AssetMaintenance: add PaymentID (nullable int) to track which payment paid for it
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'AssetMaintenance' AND COLUMN_NAME = 'PaymentID'
)
BEGIN
    ALTER TABLE AssetMaintenance
    ADD PaymentID INT NULL;
END

-- 3. Stored procedure: get maintenance records due for payment by vendor (supplier)
IF OBJECT_ID('sp_GetMaintenanceForPayment', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetMaintenanceForPayment;
GO

CREATE PROCEDURE sp_GetMaintenanceForPayment
    @VendorName NVARCHAR(255),
    @PaymentID  INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        am.ID                           AS MaintenanceID,
        0                               AS PaymentDetailID,
        ISNULL(bp.PaymentID, 0)         AS PaymentID,
        am.MaintenanceNo                AS MaintenanceRef,
        am.AssetID,
        a.AssetCode,
        a.Name                          AS AssetName,
        am.VendorName,
        am.StartDate,
        ISNULL(am.MaintenanceCost, 0)   AS TotalAmount,
        -- PaidAmount = sum of all non-deleted payment detail rows for this maintenance
        ISNULL((
            SELECT SUM(bpd2.Amount)
            FROM BranchPaymentDetails bpd2
            WHERE bpd2.MaintenanceID = am.ID
              AND ISNULL(bpd2.IsDeleted, 0) = 0
        ), 0)                           AS PaidAmount,
        am.PaymentID                    AS LinkedPaymentID
    FROM AssetMaintenance am
    JOIN AssetMaster a ON a.ID = am.AssetID
    LEFT JOIN BranchPaymentDetails bp ON bp.MaintenanceID = am.ID
                                      AND bp.PaymentID = @PaymentID
                                      AND ISNULL(bp.IsDeleted,0) = 0
    WHERE am.IsActive = 1
      AND am.VendorName = @VendorName
      AND ISNULL(am.MaintenanceCost, 0) > 0
      AND am.Status = 'Completed'       -- Only completed maintenance
      AND (
            -- unpaid rows
            ISNULL((
                SELECT SUM(bpd3.Amount)
                FROM BranchPaymentDetails bpd3
                WHERE bpd3.MaintenanceID = am.ID
                  AND ISNULL(bpd3.IsDeleted,0)=0
            ),0) < ISNULL(am.MaintenanceCost,0)
            -- OR rows already linked to this payment (for edit mode)
            OR (@PaymentID > 0 AND EXISTS(
                SELECT 1 FROM BranchPaymentDetails bpd4
                WHERE bpd4.MaintenanceID = am.ID
                  AND bpd4.PaymentID = @PaymentID
                  AND ISNULL(bpd4.IsDeleted,0)=0
            ))
      )
    ORDER BY am.StartDate, am.MaintenanceNo;
END
GO
