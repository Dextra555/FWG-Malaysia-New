using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ClientInvoiceDetails")]
    public class ClientInvoiceDetail
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public decimal ClientInvoiceID { get; set; }

        [Required]
        public decimal AgreementDetailID { get; set; }

        [Required]
        public decimal AgreementID { get; set; }

        [Required]
        public DateTime AgreementDate { get; set; }

        [Required]
        public int NoOfGuards { get; set; }

        [Required]
        public decimal Rate { get; set; }

        [Required]
        public decimal NoOfHours { get; set; }

        [Required]
        public decimal NoOfDays { get; set; }

        [Required]
        public bool FollowCalender { get; set; }

        [Required]
        public bool HasDiscount { get; set; }

        [Required]
        public decimal DiscountAmount { get; set; }

        [Required]
        public bool IsTaxable { get; set; }

        [Required]
        public decimal TaxAmount { get; set; }

        [Required]
        public DateTime LASTUPDATE { get; set; }

        [StringLength(20)]
        public string? LastUpdatedBy { get; set; }

        [Required]
        public decimal MonthTotal { get; set; }

    }
}
