using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ClientInvoice")]
    public class ClientInvoice
    {

        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(20)]
        public string InvoiceNo { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        [Required]
        [StringLength(500)]
        public string Subject { get; set; }

        [Required]
        public decimal ServiceCharges { get; set; }

        [Required]
        public decimal Discount { get; set; }

        [Required]
        public decimal TaxAmount { get; set; }

        [Required]
        [StringLength(4000)]
        public string Note { get; set; }

        [Required]
        [StringLength(20)]
        public string Branch { get; set; }

        [Required]
        [StringLength(20)]
        public string Client { get; set; }

        [Required]
        [StringLength(1)]
        public string IsDeleted { get; set; }

        [Required]
        public int AgreementID { get; set; }

        [Required]
        public DateTime LASTUPDATE { get; set; }

        [StringLength(20)]
        public string? LastUpdatedBy { get; set; }
    }
}
