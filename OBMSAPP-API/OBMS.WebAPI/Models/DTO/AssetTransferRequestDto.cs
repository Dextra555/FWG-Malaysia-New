namespace OBMS.WebAPI.Models.DTO
{
    public class AssetTransferRequestDto
    {
        public int ID { get; set; }
        public string? TransferNo { get; set; }
        public int AssetID { get; set; }
        public DateTime TransferDate { get; set; }
        public string FromType { get; set; } = string.Empty;
        public int? FromID { get; set; }
        public string? FromName { get; set; }
        public string ToType { get; set; } = string.Empty;
        public int? ToID { get; set; }
        public string? ToName { get; set; }
        public string? TransferReason { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class AssetTransferListDto
    {
        public int ID { get; set; }
        public string TransferNo { get; set; } = string.Empty;
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public DateTime TransferDate { get; set; }
        public string FromType { get; set; } = string.Empty;
        public string? FromName { get; set; }
        public string ToType { get; set; } = string.Empty;
        public string? ToName { get; set; }
        public string? TransferReason { get; set; }
        public string? Remarks { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
