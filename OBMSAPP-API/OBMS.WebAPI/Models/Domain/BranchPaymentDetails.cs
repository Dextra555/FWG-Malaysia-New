using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("BranchPaymentDetails")]
    public class BranchPaymentDetails
    {
        [Key]
        public int ID { get; set; }
        public decimal PaymentID { get; set; }
        public string Branch { get; set; }
        public decimal? InvoiceID { get; set; }
        /// <summary>
        /// Optional GRN reference. Populated when this payment line is
        /// paying a Goods Receipt Note instead of a CreditorInvoice.
        /// </summary>
        public int? GRNID { get; set; }
        public decimal Amount { get; set; }
        public DateTime LastUpdate { get; set; }
        public string LastUpdatedBy { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
