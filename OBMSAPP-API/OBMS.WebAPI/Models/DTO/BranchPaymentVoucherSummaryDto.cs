namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// DTO for Branch Payment Summary grouped by Voucher Number.
    /// Each record represents one unique VoucherNo with accumulated totals.
    /// </summary>
    public class BranchPaymentVoucherSummaryDto
    {
        public string VoucherNo { get; set; }
        public string Branch { get; set; }
        public DateTime PaymentDate { get; set; }
        public int CreditorType { get; set; }
        public int PaymentType { get; set; }
        public int PaymentPurpose { get; set; }
        public string PaymentTo { get; set; }
        public string Particulars { get; set; }
        public string ItemCategory { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime FirstPaymentDate { get; set; }
        public DateTime LastPaymentDate { get; set; }
    }
}
