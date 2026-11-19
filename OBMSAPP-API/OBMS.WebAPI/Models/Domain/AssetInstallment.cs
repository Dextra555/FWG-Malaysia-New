using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// One row per monthly installment for an Asset purchased on MONTHLY payment terms.
    /// Each row has a linked CreditorInvoice (InvoiceType='AI') so Finance → Payment
    /// can display and pay it under the Purchase category.
    /// </summary>
    [Table("AssetInstallment")]
    public class AssetInstallment
    {
        [Key]
        public int ID { get; set; }

        public int AssetID { get; set; }

        /// <summary>Sequence number within this asset's schedule: 1, 2, 3 …</summary>
        public int InstallmentNo { get; set; }

        /// <summary>Payment due date (1st of the month).</summary>
        public DateTime DueDate { get; set; }

        public decimal Amount { get; set; }

        /// <summary>Pending | Paid</summary>
        public string Status { get; set; } = "Pending";

        /// <summary>FK to CreditorInvoice row created for this installment (InvoiceType='AI').</summary>
        public int? CreditorInvoiceID { get; set; }

        /// <summary>Set when this installment is settled via Finance → Payment.</summary>
        public int? PaymentID { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Navigation
        [ForeignKey("AssetID")]
        public virtual AssetMaster? Asset { get; set; }
    }
}
