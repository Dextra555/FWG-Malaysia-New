using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("InvoiceDetails")]
    public class InvoiceDetails
    {
        [Key]
        public int ID { get; set; }
        public string InvoiceNo { get; set; }
        public string Branch { get; set; }
        public string Client { get; set; }
        public string BankCode { get; set; }
        public string BankBranch { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal ServiceCharges { get; set; }
        public decimal Discount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal InvoiceAmount { get; set; }
        public bool IsDeleted { get; set; }
        public decimal? Payment { get; set; }
        public decimal? CreditNoteAmount { get; set; }
        public decimal? Balance { get; set; }
        public decimal? SuspendAmount { get; set; }
        public decimal? DebitNoteAmount { get; set; }
    }
}
