using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("DebitNote")]
    public class DebitNote
    {
        [Key]
        public int ID { get; set; }

        [Required]
        [StringLength(50)]
        public string DebitNoteNo { get; set; }

        [Required]
        [StringLength(20)]
        public string Branch { get; set; }

        [Required]
        [StringLength(50)]
        public string Client { get; set; }

        public int? ClientInvoiceID { get; set; }

        [Required]
        public DateTime DebitNoteDate { get; set; }

        [Required]
        public decimal DebitNoteAmount { get; set; }

        public decimal TaxPercentage { get; set; } = 0;

        public decimal TaxAmount { get; set; } = 0;

        public decimal TotalAmount { get; set; } = 0;

        [StringLength(1000)]
        public string? Reason { get; set; }

        [StringLength(50)]
        public string? ReferenceInvoiceNo { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastUpdatedDate { get; set; }

        [StringLength(50)]
        public string? LastUpdatedBy { get; set; }

        public bool IsDeleted { get; set; } = false;

        // Not mapped — populated at query time for list display
        [NotMapped]
        public string? ClientName { get; set; }
    }
}
