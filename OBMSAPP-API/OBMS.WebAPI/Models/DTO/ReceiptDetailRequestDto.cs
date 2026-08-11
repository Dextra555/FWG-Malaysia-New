namespace OBMS.WebAPI.Models.DTO
{
    public class ReceiptDetailRequestDto
    {
        public int ID { get; set; }
        public decimal ReceiptID { get; set; }
        public decimal InvoiceID { get; set; }
        public string InvoiceNo { get; set; }
        public decimal Amount { get; set; }
        public int BalanceStatus { get; set; }
        public decimal BalanceAmount { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
