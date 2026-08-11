using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ReceiptClientInvoiceView")]
    [Keyless]
    public class ReceiptClientInvoiceView
    {
        public int InvoiceID { get; set; }
        public string InvoiceNo { get; set; }
        public decimal InvoiceAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal PaidAmount { get; set; }
        public string Branch { get; set; }
        public string Client { get; set; }
    }
}
