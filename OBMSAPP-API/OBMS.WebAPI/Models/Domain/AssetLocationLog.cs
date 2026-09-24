using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Append-only log of every asset location update.
    /// Used to track where company vehicles/assets are parked,
    /// especially when Guards park at unexpected/defect locations.
    ///
    /// LocationStatus values:
    ///   Normal          — asset at expected/base location
    ///   Defect_Location — parked at wrong/unexpected location
    ///   Parked_Away     — Guard parked away from duty point
    ///   Unknown         — location not confirmed
    /// </summary>
    [Table("AssetLocationLog")]
    public class AssetLocationLog
    {
        [Key]
        public int ID { get; set; }

        public int AssetID { get; set; }

        public DateTime LogDate { get; set; }

        public string? PreviousLocation { get; set; }

        public string NewLocation { get; set; } = string.Empty;

        /// <summary>Normal | Defect_Location | Parked_Away | Unknown</summary>
        public string LocationStatus { get; set; } = "Normal";

        /// <summary>Guard or staff who reported / updated the location</summary>
        public string? UpdatedBy { get; set; }

        public string? Remarks { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        [ForeignKey("AssetID")]
        public virtual AssetMaster? Asset { get; set; }
    }
}
