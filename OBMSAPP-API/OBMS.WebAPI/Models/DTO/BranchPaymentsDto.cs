namespace OBMS.WebAPI.Models.DTO
{
    public class BranchPaymentsDto
    {
        public string VoucherNo { get; set; }
        public DateTime PaymentDate { get; set; }
        public string SupplierName { get; set; }
        public string BankCode { get; set; }
        public string ChequeNo { get; set; }
        public string PaymentTo { get; set; }
        public string Particulars { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }

        public BranchPaymentsDto(
            string voucherNo,
            DateTime paymentDate,
            string supplierName,
            string bankCode,
            string chequeNo,
            string paymentTo,
            string particulars,
            decimal amount,
            string category)
        {
            VoucherNo = voucherNo;
            PaymentDate = paymentDate;
            SupplierName = supplierName;
            BankCode = bankCode;
            ChequeNo = chequeNo;
            PaymentTo = paymentTo;
            Particulars = particulars;
            Amount = amount;
            Category = category;
        }
    }

}
