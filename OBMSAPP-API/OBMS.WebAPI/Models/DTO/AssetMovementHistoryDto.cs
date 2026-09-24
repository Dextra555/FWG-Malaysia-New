namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>One row in the asset movement history timeline.</summary>
    public class AssetMovementHistoryDto
    {
        public int ID { get; set; }
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public DateTime MovementDate { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string? FromPerson { get; set; }
        public string? ToPerson { get; set; }
        public string? ReferenceNo { get; set; }
        public string? ReferenceType { get; set; }
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
    }
}
