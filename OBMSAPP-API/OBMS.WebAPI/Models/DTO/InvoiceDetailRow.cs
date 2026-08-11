namespace OBMS.WebAPI.Models.DTO
{
    public class InvoiceDetailRow
    {
        public int ID { get; set; }
        public int ReceiptID { get; set; }
        public int InvoiceID { get; set; }
        public string InvoiceNo { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime InvoiceDate { get; set; }

        // optional calculated field
        public decimal BalanceAmount { get; set; }
    }

}
