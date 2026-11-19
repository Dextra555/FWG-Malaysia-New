using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Maintenance schedule and history for an asset.
    /// On save: Asset.AssetStatus → Under_Maintenance.
    /// On complete: Asset.AssetStatus → Available (or Allocated if was allocated before).
    /// Disposed assets cannot enter maintenance.
    /// </summary>
    [Table("AssetMaintenance")]
    public class AssetMaintenance
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated, e.g. AM0001</summary>
        public string MaintenanceNo { get; set; } = string.Empty;

        public int AssetID { get; set; }

        /// <summary>Preventive | Corrective | Emergency</summary>
        public string MaintenanceType { get; set; } = string.Empty;

        public string? ProblemDescription { get; set; }
        public string? VendorName { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }

        public decimal? MaintenanceCost { get; set; } = 0;

        /// <summary>Scheduled | In_Progress | Completed | Cancelled</summary>
        public string Status { get; set; } = "Scheduled";

        public string? CompletionRemarks { get; set; }
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        [ForeignKey("AssetID")]
        public virtual AssetMaster? Asset { get; set; }
    }
}
