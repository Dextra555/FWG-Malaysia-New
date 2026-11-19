namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload to update the current location of an asset.
    /// Used when a Guard parks a company vehicle at a different location.
    /// </summary>
    public class AssetLocationUpdateDto
    {
        public int AssetID { get; set; }

        public string NewLocation { get; set; } = string.Empty;

        /// <summary>Normal | Defect_Location | Parked_Away | Unknown</summary>
        public string LocationStatus { get; set; } = "Normal";

        /// <summary>Guard / staff name who is reporting the location</summary>
        public string? UpdatedBy { get; set; }

        public string? Remarks { get; set; }

        public string? LastUpdatedBy { get; set; }
    }

    /// <summary>One row in the location history log.</summary>
    public class AssetLocationLogDto
    {
        public int ID { get; set; }
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public DateTime LogDate { get; set; }
        public string? PreviousLocation { get; set; }
        public string NewLocation { get; set; } = string.Empty;
        public string LocationStatus { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
