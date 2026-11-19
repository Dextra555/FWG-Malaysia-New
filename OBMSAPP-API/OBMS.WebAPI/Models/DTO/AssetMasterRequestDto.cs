namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Payload for creating or updating an AssetMaster record.
    /// ID = 0 means new; ID > 0 means update.
    /// AssetCode is generated server-side on new records.
    /// </summary>
    public class AssetMasterRequestDto
    {
        public int ID { get; set; }

        /// <summary>Server-generated on new (AST0001). Preserved on edit.</summary>
        public string? AssetCode { get; set; }

        public string? Name { get; set; }

        /// <summary>Fixed Asset | Liquid Assets | Current Liabilities | Long Term Liabilities</summary>
        public string? AssetType { get; set; }

        public string? SerialNumber { get; set; }
        public string? Description { get; set; }
        public string? ModelNo { get; set; }
        public string? BrandName { get; set; }
        public int? SupplierID { get; set; }

        /// <summary>Branch code (free text — existing field)</summary>
        public string? Branch { get; set; }

        public double? PurchaseAmount { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }
        public string? AssetCondition { get; set; }
        public string? Remarks { get; set; }

        public DateTime? LastUpdatedDate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
