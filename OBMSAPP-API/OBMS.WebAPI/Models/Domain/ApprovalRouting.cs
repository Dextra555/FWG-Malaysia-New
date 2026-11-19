using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// User-wise approval routing.
    /// Maps (DocumentType + RequesterUser) → ApproverUser.
    ///
    /// A requester may have ONE active approver per document type.
    /// Unique enforcement is applied at the database level via a
    /// filtered unique index on (DocumentType, RequesterUser) WHERE IsActive = 1.
    /// </summary>
    [Table("ApprovalRouting")]
    public class ApprovalRouting
    {
        [Key]
        public int ID { get; set; }

        /// <summary>PURCHASE_REQUISITION | PURCHASE_ORDER | STOCK_ADJUSTMENT | …</summary>
        [Required, MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>OBMSUsers.Name of the user who submits the document.</summary>
        [Required, MaxLength(200)]
        public string RequesterUser { get; set; } = string.Empty;

        /// <summary>OBMSUsers.Name of the designated approver for this requester.</summary>
        [Required, MaxLength(200)]
        public string ApproverUser { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [MaxLength(200)]
        public string? LastUpdatedBy { get; set; }

        public DateTime LASTUPDATE { get; set; } = DateTime.Now;
    }
}
