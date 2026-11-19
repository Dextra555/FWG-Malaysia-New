using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Records a damage incident for an asset.
    /// On save: Asset.AssetStatus → Damaged.
    /// Does NOT auto-dispose — user must explicitly create AssetDisposal.
    /// After recording damage, the user decides: send to Maintenance or Dispose.
    /// </summary>
    [Table("AssetDamage")]
    public class AssetDamage
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated, e.g. AD0001</summary>
        public string DamageNo { get; set; } = string.Empty;

        public int AssetID { get; set; }
        public DateTime DamageDate { get; set; }

        /// <summary>Physical | Water | Electrical | Fire | Other</summary>
        public string? DamageType { get; set; }

        public string DamageDescription { get; set; } = string.Empty;

        /// <summary>Repairable | Beyond_Repair</summary>
        public string? DamageCondition { get; set; }

        public string? ReportedBy { get; set; }
        public decimal? EstimatedRepairCost { get; set; } = 0;
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        [ForeignKey("AssetID")]
        public virtual AssetMaster? Asset { get; set; }
    }
}
