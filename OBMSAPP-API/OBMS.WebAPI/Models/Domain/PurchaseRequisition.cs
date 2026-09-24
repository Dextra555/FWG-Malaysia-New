using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("PurchaseRequisition")]
    public class PurchaseRequisition
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated: PR0001, PR0002 …</summary>
        public string PRNumber { get; set; } = string.Empty;

        public DateTime PRDate { get; set; }

        public DateTime RequiredByDate { get; set; }

        /// <summary>Draft | Submitted | Approved | Rejected</summary>
        public string Status { get; set; } = "Draft";

        public string? Remarks { get; set; }

        public string? SubmittedBy { get; set; }

        public DateTime? SubmittedDate { get; set; }

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        /// <summary>Filled by approver on Approve or Reject action.</summary>
        public string? ApprovalRemarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation — JsonIgnore prevents circular reference in API responses
        [JsonIgnore]
        public virtual ICollection<PurchaseRequisitionDetail> Details { get; set; }
            = new List<PurchaseRequisitionDetail>();
    }
}
