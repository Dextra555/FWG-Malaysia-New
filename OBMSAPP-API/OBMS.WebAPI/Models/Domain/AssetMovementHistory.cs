using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Immutable audit trail — one row per state change.
    /// Never updated after insert. All asset operations append here.
    /// MovementType values:
    ///   Allocation | Transfer | Return |
    ///   Maintenance | Maintenance_Complete |
    ///   Damage | Disposal | Status_Change
    /// </summary>
    [Table("AssetMovementHistory")]
    public class AssetMovementHistory
    {
        [Key]
        public int ID { get; set; }

        public int AssetID { get; set; }

        public DateTime MovementDate { get; set; }

        public string MovementType { get; set; } = string.Empty;

        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string? FromPerson { get; set; }
        public string? ToPerson { get; set; }

        /// <summary>e.g. AA0001, AT0001 — the reference doc number</summary>
        public string? ReferenceNo { get; set; }

        /// <summary>Allocation | Transfer | Return | Maintenance | Damage | Disposal</summary>
        public string? ReferenceType { get; set; }

        public int? ReferenceID { get; set; }

        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }

        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }

        public DateTime LASTUPDATE { get; set; }

        [ForeignKey("AssetID")]
        public virtual AssetMaster? Asset { get; set; }
    }
}
