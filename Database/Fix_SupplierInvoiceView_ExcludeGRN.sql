-- ═══════════════════════════════════════════════════════════════════════════
-- Fix: SupplierInvoiceView — Exclude GRN rows (InvoiceType = 'G')
--
-- Background:
--   GRN payments now insert a row into CreditorInvoice with InvoiceType = 'G'.
--   The SupplierInvoiceView is used by the Expenses payment flow (Finance →
--   Payments → CreditorType=Suppliers → Category=Expenses). GRN rows must NOT
--   appear there; they are handled separately via Finance/GetGRNsForPayment.
--
-- Change:
--   Add   AND dbo.CreditorInvoice.InvoiceType <> 'G'
--   to the WHERE clause so GRN-linked CreditorInvoice rows are excluded.
-- ═══════════════════════════════════════════════════════════════════════════

ALTER VIEW [dbo].[SupplierInvoiceView] AS

SELECT DISTINCT
    TOP (100) PERCENT
    dbo.CreditorInvoice.ID,
    dbo.BranchMaster.Name          AS Branchname,
    dbo.Suppliers.Name             AS Suppliername,
    dbo.CreditorInvoice.InvoiceNo,
    dbo.CreditorInvoice.PaymentDate AS Dudate,
    dbo.CreditorInvoice.Total,
    ISNULL(dbo.PaidAmount(dbo.CreditorInvoice.ID), 0) AS Amount,
    dbo.OBMSBranches.Name          AS UserId

FROM dbo.CreditorInvoice
INNER JOIN dbo.Suppliers
    ON dbo.CreditorInvoice.Supplier    = dbo.Suppliers.Id
INNER JOIN dbo.BranchMaster
    ON dbo.CreditorInvoice.Branch      = dbo.BranchMaster.Code
INNER JOIN dbo.OBMSBranches
    ON dbo.BranchMaster.Code           = dbo.OBMSBranches.BranchCode

WHERE
    YEAR(dbo.CreditorInvoice.PaymentDate) >= 2014
    AND dbo.CreditorInvoice.PaymentDate  <= GETDATE()
    AND dbo.CreditorInvoice.Total
            > ISNULL(dbo.PaidAmount(dbo.CreditorInvoice.ID), 0)
    AND dbo.CreditorInvoice.IsDeleted    = 0
    AND dbo.CreditorInvoice.InvoiceType <> 'G'   -- ← Exclude GRN rows

ORDER BY dbo.CreditorInvoice.ID;
GO
