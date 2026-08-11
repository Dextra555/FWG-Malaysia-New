using OBMS.WebAPI.Models.Domain;

namespace OBMS.WebAPI.Models.DTO
{
    public class ClientInvoiceListDto
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
        public string? WorkPlace { get; set; }

        // ✅ Default constructor
        public ClientInvoiceListDto() { }

        // ✅ Full constructor
        public ClientInvoiceListDto(
            decimal row,
            string invoiceDate,
            string branchName,
            string clientName,
            string invoiceNumber,
            decimal serviceCharges,
            decimal discount,
            decimal taxAmount,
            decimal invoiceAmount,
            string payment
        )
        {
            Row = row;
            InvoiceDate = invoiceDate;
            BranchName = branchName;
            ClientName = clientName;
            InvoiceNumber = invoiceNumber;
            ServiceCharges = serviceCharges;
            Discount = discount;
            TaxAmount = taxAmount;
            InvoiceAmount = invoiceAmount;
            Payment = payment;
        }
    }

}
