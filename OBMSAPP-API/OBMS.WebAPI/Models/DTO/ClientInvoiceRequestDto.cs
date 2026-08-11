namespace OBMS.WebAPI.Models.DTO
{
    public class ClientInvoiceRequestDto
    {
        public int ID { get; set; }

        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Subject { get; set; }
        public decimal ServiceCharges { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxAmount { get; set; }
        public string Note { get; set; }
        public string Branch { get; set; }
        public string Client { get; set; }
        public int AgreementID { get; set; }
        public int? DeletedDetailID { get; set; }
        public ClientInvoiceDetailRequestDto[]? details { get; set; }
    }
}
