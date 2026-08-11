using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("Receipts")]
    public class Receipts
    {
        [Key]
        public int ID { get; set; }
        public string? VoucherNo { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string? Branch { get; set; }
        public string? PaymentFrom { get; set; }
        public string? Particulars { get; set; }
        public int ReceiptType { get; set; }
        public bool IsInvoiceAdjustment { get; set; }
        public string? BankCode { get; set; }
        public string? BankBranch { get; set; }
        public string? ChequeNo { get; set; }
        public string? InvoiceNumbers { get; set; }
        public decimal ReceiptAmount { get; set; }
        public decimal TaxPercentage { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal HQPercentage { get; set; }
        public decimal HQAmount { get; set; }
        public decimal BranchCollection { get; set; }
        public decimal CreditNoteAmount { get; set; }
        public decimal DebitNoteAmount { get; set; }
        public decimal SuspendAmount { get; set; }
        public decimal BankID { get; set; }
        public char ChequeStatus { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
