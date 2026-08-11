namespace OBMS.WebAPI.Models.Domain
{
    public class ReceiptDetail
    {
        public int ID { get; set; }
        public decimal ReceiptID { get; set; }
        public decimal InvoiceID { get; set; }
        public decimal Amount { get; set; }
        public int BalanceStatus { get; set; }
        public decimal BalanceAmount { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
