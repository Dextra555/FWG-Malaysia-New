namespace OBMS.WebAPI.Models.DTO
{
    public class AssetDamageRequestDto
    {
        public int ID { get; set; }
        public string? DamageNo { get; set; }
        public int AssetID { get; set; }
        public DateTime DamageDate { get; set; }

        /// <summary>Physical | Water | Electrical | Fire | Other</summary>
        public string? DamageType { get; set; }

        public string DamageDescription { get; set; } = string.Empty;

        /// <summary>Repairable | Beyond_Repair</summary>
        public string? DamageCondition { get; set; }

        public string? ReportedBy { get; set; }
        public decimal? EstimatedRepairCost { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class AssetDamageListDto
    {
        public int ID { get; set; }
        public string DamageNo { get; set; } = string.Empty;
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public DateTime DamageDate { get; set; }
        public string? DamageType { get; set; }
        public string DamageDescription { get; set; } = string.Empty;
        public string? DamageCondition { get; set; }
        public string? ReportedBy { get; set; }
        public decimal? EstimatedRepairCost { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
