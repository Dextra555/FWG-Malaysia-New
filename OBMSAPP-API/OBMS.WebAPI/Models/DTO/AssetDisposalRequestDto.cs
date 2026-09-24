namespace OBMS.WebAPI.Models.DTO
{
    public class AssetDisposalRequestDto
    {
        public int ID { get; set; }
        public string? DisposalNo { get; set; }
        public int AssetID { get; set; }
        public DateTime DisposalDate { get; set; }

        /// <summary>Sold | Scrapped | Donated | Written_Off</summary>
        public string DisposalType { get; set; } = string.Empty;

        public string DisposalReason { get; set; } = string.Empty;
        public decimal? DisposalValue { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class AssetDisposalListDto
    {
        public int ID { get; set; }
        public string DisposalNo { get; set; } = string.Empty;
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public DateTime DisposalDate { get; set; }
        public string DisposalType { get; set; } = string.Empty;
        public string DisposalReason { get; set; } = string.Empty;
        public decimal? DisposalValue { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
