namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Represents a single asset installment row as returned to the Finance Payment page.
    /// Mirrors the shape of GRNPaymentRowDto so the same Angular table can display it.
    /// </summary>
    public class AssetInstallmentPaymentRowDto
    {
        // ── Identity ──────────────────────────────────────────────────────────
        public int InstallmentID { get; set; }           // AssetInstallment.ID
        public int PaymentDetailID { get; set; }         // BranchPaymentDetails.ID (0 if new)
        public int PaymentID { get; set; }               // BranchPaymentDetails.PaymentID (0 if new)
        public int? CreditorInvoiceID { get; set; }      // CreditorInvoice.ID for this installment

        // ── Asset info ────────────────────────────────────────────────────────
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public int? SupplierID { get; set; }
        public string? SupplierName { get; set; }
        public string? BranchCode { get; set; }

        // ── Installment info ──────────────────────────────────────────────────
        public int InstallmentNo { get; set; }
        public DateTime DueDate { get; set; }
        public string InstallmentRef { get; set; } = string.Empty;  // e.g. "AST0001 – Inst. 1 of 12"

        // ── Payment amounts ───────────────────────────────────────────────────
        public decimal TotalAmount { get; set; }         // installment amount
        public decimal PaidAmount { get; set; }          // already paid (0 or TotalAmount)
        public decimal Balance { get; set; }             // remaining
        public decimal Amount { get; set; }              // current payment entry (editable)

        public string Status { get; set; } = "Pending";
    }
}
