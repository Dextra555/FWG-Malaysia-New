using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Permanently retires an asset from service.
    /// On save: Asset.AssetStatus → Disposed, Asset.IsActive → false.
    /// Validations: cannot dispose an already-disposed asset;
    ///              active allocations must be resolved first.
    /// </summary>
    [Table("AssetDisposal")]
    public class AssetDisposal
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated, e.g. DIS0001</summary>
        public string DisposalNo { get; set; } = string.Empty;

        public int AssetID { get; set; }
        public DateTime DisposalDate { get; set; }

        /// <summary>Sold | Scrapped | Donated | Written_Off</summary>
        public string DisposalType { get; set; } = string.Empty;

        public string DisposalReason { get; set; } = string.Empty;

        public decimal? DisposalValue { get; set; } = 0;
        public string? ApprovedBy { get; set; }
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        // ── Approval workflow ────────────────────────────────────────────────
        /// <summary>Draft | Submitted | Approved | Rejected</summary>
        public string Status { get; set; } = "Draft";
        public string? SubmittedBy     { get; set; }
        public DateTime? SubmittedDate  { get; set; }
        public DateTime? ApprovedDate   { get; set; }
        public string? ApprovalRemarks { get; set; }

        [ForeignKey("AssetID")]
        public virtual AssetMaster? Asset { get; set; }
    }
}
