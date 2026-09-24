namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>Enriched asset row for list and detail views.</summary>
    public class AssetListDto
    {
        public int ID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? AssetType { get; set; }
        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public string? ModelNo { get; set; }
        public string? BrandName { get; set; }
        public int? SupplierID { get; set; }
        public string? SupplierName { get; set; }
        public string? Branch { get; set; }
        public double? PurchaseAmount { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }
        public string AssetStatus { get; set; } = "Available";
        public string? AssetCondition { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }

        // Current allocation info (null if not allocated)
        public string? CurrentAllocatedTo { get; set; }
        public string? CurrentAllocatedType { get; set; }

        // Current location tracking
        public string? CurrentLocation { get; set; }
        public string? LocationStatus { get; set; }
        public string? LocationUpdatedBy { get; set; }
        public DateTime? LocationUpdatedDate { get; set; }
    }
}
