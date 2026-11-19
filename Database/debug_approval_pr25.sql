-- ============================================================
-- DEBUG: Approval Routing Bug — PR 25 Investigation
-- Run this on your database to see the exact values.
-- ============================================================

-- 1. Check PR 25 exactly
SELECT
    ID,
    PRNumber,
    Status,
    SubmittedBy,
    SubmittedDate,
    LastUpdatedBy,
    LASTUPDATE,
    ApprovedBy,
    ApprovalRemarks
FROM PurchaseRequisition
WHERE ID = 25;

-- 2. Check ALL ApprovalRoutings for PURCHASE_REQUISITION (active + inactive)
SELECT
    ID,
    DocumentType,
    RequesterUser,
    ApproverUser,
    IsActive,
    LASTUPDATE
FROM ApprovalRouting
WHERE DocumentType = 'PURCHASE_REQUISITION'
ORDER BY IsActive DESC, RequesterUser;

-- 3. Cross-check: does PR 25's SubmittedBy match any ApprovalRouting.RequesterUser?
SELECT
    pr.ID,
    pr.PRNumber,
    pr.Status,
    pr.SubmittedBy              AS PR_SubmittedBy,
    pr.LastUpdatedBy            AS PR_LastUpdatedBy,
    ar.RequesterUser            AS Routing_RequesterUser,
    ar.ApproverUser             AS Routing_ApproverUser,
    ar.IsActive                 AS Routing_IsActive,
    CASE
        WHEN pr.SubmittedBy COLLATE SQL_Latin1_General_CP1_CI_AS
           = ar.RequesterUser  COLLATE SQL_Latin1_General_CP1_CI_AS
        THEN 'MATCH (CI)'
        WHEN pr.SubmittedBy = ar.RequesterUser
        THEN 'MATCH (CS)'
        ELSE 'NO MATCH'
    END                         AS MatchResult
FROM PurchaseRequisition pr
LEFT JOIN ApprovalRouting ar
    ON ar.DocumentType = 'PURCHASE_REQUISITION'
    AND ar.IsActive = 1
WHERE pr.ID = 25;

-- 4. Check OBMSUsers — exact Name casing stored
SELECT
    UserId,
    Name,
    IsDeleted
FROM OBMSUsers
WHERE IsDeleted = 0
ORDER BY Name;

-- 5. IMPORTANT: Check what OBMSPermissions Dharan & Valli have for these screens
SELECT
    Name,
    ScreenName,
    [Read],
    [Create],
    [Update],
    [Delete]
FROM OBMSPermissions
WHERE ScreenName IN ('Purchase Requisition', 'Pending Approvals', 'Approval Setup')
ORDER BY ScreenName, Name;
