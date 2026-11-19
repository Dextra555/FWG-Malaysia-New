namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Represents one GRN row shown in the Finance → Payments page
    /// when the user chooses to pay against a GRN reference.
    /// Mirrors the shape of CreditorInvoicePaymentRow so the Angular
    /// SupplierInvoice form-array can reuse the same table.
    /// </summary>
    public class GRNPaymentRowDto
    {
        /// <summary>BranchPaymentDetails.ID (0 = not yet saved)</summary>
        public int PaymentDetailID { get; set; }

        /// <summary>BranchPaymentDetails.PaymentID (0 = new payment)</summary>
        public int PaymentID { get; set; }

        // ── GRN identity ────────────────────────────────────────────────
        public int    GRNID        { get; set; }
        public string GRNNumber    { get; set; } = string.Empty;
        public DateTime GRNDate    { get; set; }
        /// <summary>Supplier's own invoice number entered on the GRN.</summary>
        public string? SupplierInvoiceNo { get; set; }

        // ── PO / Supplier context ────────────────────────────────────────
        public string PONumber          { get; set; } = string.Empty;
        public int    SupplierID        { get; set; }
        public string SupplierName      { get; set; } = string.Empty;

        // ── Warehouse / Branch ───────────────────────────────────────────
        /// <summary>
        /// BranchMaster.Code of the Warehouse linked to this GRN.
        /// Used as BranchPaymentDetails.Branch when saving the payment.
        /// </summary>
        public string WarehouseBranchCode { get; set; } = string.Empty;

        // ── Amounts ─────────────────────────────────────────────────────
        /// <summary>SUM(ReceivedQty * UnitCost) across all GRN detail lines.</summary>
        public decimal TotalAmount  { get; set; }

        /// <summary>
        /// Total already paid for this GRN across all non-deleted payments,
        /// excluding the current payment being edited.
        /// </summary>
        public decimal PaidAmount   { get; set; }

        /// <summary>TotalAmount – PaidAmount</summary>
        public decimal Balance      { get; set; }

        /// <summary>
        /// Amount entered for the current payment (pre-filled from an
        /// existing BranchPaymentDetails row when editing; 0 for new).
        /// </summary>
        public decimal Amount       { get; set; }
    }
}
