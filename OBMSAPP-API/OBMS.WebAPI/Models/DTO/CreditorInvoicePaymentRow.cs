namespace OBMS.WebAPI.Models.DTO
{
    public class CreditorInvoicePaymentRow
    {
        public int ID { get; set; }
        public int PaymentID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int InvoiceID { get; set; }
        public string InvoiceNo { get; set; }
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal? Amount { get; set; }
    }

}
