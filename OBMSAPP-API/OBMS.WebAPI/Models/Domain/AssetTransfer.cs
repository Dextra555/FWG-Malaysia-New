using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Records movement of an asset from one location/person to another.
    /// Source and destination must be different.
    /// On save: updates AssetAllocation, movement history row created.
    /// </summary>
    [Table("AssetTransfer")]
    public class AssetTransfer
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated, e.g. AT0001</summary>
        public string TransferNo { get; set; } = string.Empty;

        public int AssetID { get; set; }
        public DateTime TransferDate { get; set; }

        /// <summary>Branch | Staff | Guard</summary>
        public string FromType { get; set; } = string.Empty;
        public int? FromID { get; set; }
        public string? FromName { get; set; }

        /// <summary>Branch | Staff | Guard</summary>
        public string ToType { get; set; } = string.Empty;
        public int? ToID { get; set; }
        public string? ToName { get; set; }

        public string? TransferReason { get; set; }
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        [ForeignKey("AssetID")]
        public virtual AssetMaster? Asset { get; set; }
    }
}
