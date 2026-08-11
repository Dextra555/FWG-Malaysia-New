namespace OBMS.WebAPI.Models.DTO
{
    public class BranchPaymentRequestDto
    {
        public int ID { get; set; }
        public string? VoucherNo { get; set; }
        public DateTime PaymentDate { get; set; }
        public int CreditorType { get; set; }
        public int PaymentType { get; set; }
        public decimal? Supplier { get; set; }
        public int PaymentPurpose { get; set; }
        public decimal? BankID { get; set; }
        public string ChequeNo { get; set; }
        public string PaymentTo { get; set; }
        public string Particulars { get; set; }
        public decimal Amount { get; set; }
        public bool IsDeleted { get; set; }
        public char? ChequeStatus { get; set; }
        public DateTime? LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime? ChqClearencedate { get; set; }
        public string ItemCategory { get; set; }

        public string? userId { get; set; }
        public BranchPaymentDetailsRequestDto[]? details { get; set; }
    }
}
