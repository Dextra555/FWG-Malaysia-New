namespace OBMS.WebAPI.Models.Domain
{
    public class ClientInvoiceList
    {
        public decimal Row { get; set; }
        public string InvoiceDate { get; set; }
        public string BranchName { get; set; }
        public string ClientName { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal ServiceCharges { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal InvoiceAmount { get; set; }
        public string Payment { get; set; }
    }
}
