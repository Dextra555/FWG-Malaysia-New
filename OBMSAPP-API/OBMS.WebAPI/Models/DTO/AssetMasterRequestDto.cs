namespace OBMS.WebAPI.Models.DTO
{
    public class AssetMasterRequestDto
    {
        public int ID { get; set; }

        public string? Name { get; set; }
        public string? AssetType { get; set; }
        public string? Branch { get; set; }
        public double? PurchaseAmount { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
