using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("CreditorInvoiceDetails")]
    public class CreditorInvoiceDetails
    {
        [Key]
        public int ID { get; set; }
        public int InvoiceID { get; set; }
        public int SerialNo { get; set; }
        public int? ItemID { get; set; }
        public int? ItemCategoryID { get; set; }
        public int? NoOfUnits { get; set; }
        public decimal? CostPerUnit { get; set; }
        public decimal? ValuePeriod { get; set; }
        public decimal? ValuePercentage { get; set; }
        public string? ValueStatus { get; set; }
        public DateTime? InvoicePeriodFrom { get; set; }
        public DateTime? InvoicePeriodTo { get; set; }
        public decimal Amount { get; set; }
        public string Note { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string LastUpdatedBy { get; set; }
    }

}
