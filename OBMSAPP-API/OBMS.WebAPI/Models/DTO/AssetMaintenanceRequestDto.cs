namespace OBMS.WebAPI.Models.DTO
{
    public class AssetMaintenanceRequestDto
    {
        public int ID { get; set; }
        public string? MaintenanceNo { get; set; }
        public int AssetID { get; set; }

        /// <summary>Preventive | Corrective | Emergency</summary>
        public string MaintenanceType { get; set; } = string.Empty;

        public string? ProblemDescription { get; set; }
        public string? VendorName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public decimal? MaintenanceCost { get; set; }
        public string? CompletionRemarks { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class AssetMaintenanceListDto
    {
        public int ID { get; set; }
        public string MaintenanceNo { get; set; } = string.Empty;
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public string MaintenanceType { get; set; } = string.Empty;
        public string? ProblemDescription { get; set; }
        public string? VendorName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public decimal? MaintenanceCost { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? CompletionRemarks { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
