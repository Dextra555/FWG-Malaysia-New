using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Records the assignment of an asset to a Branch, Staff, or Guard.
    /// Only one active (IsActive = true) allocation per asset is allowed.
    /// On save: Asset.AssetStatus → Allocated, movement history row created.
    /// </summary>
    [Table("AssetAllocation")]
    public class AssetAllocation
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated, e.g. AA0001</summary>
        public string AllocationNo { get; set; } = string.Empty;

        public int AssetID { get; set; }
        public DateTime AllocationDate { get; set; }

        /// <summary>Branch | Staff | Guard</summary>
        public string AllocatedToType { get; set; } = string.Empty;

        /// <summary>EmployeeID or internal BranchID (nullable)</summary>
        public int? AllocatedToID { get; set; }

        /// <summary>Denormalised display name for easy display</summary>
        public string? AllocatedToName { get; set; }

        /// <summary>Branch context when allocating to Staff/Guard</summary>
        public string? BranchCode { get; set; }

        public string? Remarks { get; set; }

        /// <summary>True = currently active allocation</summary>
        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        [ForeignKey("AssetID")]
        public virtual AssetMaster? Asset { get; set; }
    }
}
