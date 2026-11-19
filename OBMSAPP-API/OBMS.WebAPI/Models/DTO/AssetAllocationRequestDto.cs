namespace OBMS.WebAPI.Models.DTO
{
    public class AssetAllocationRequestDto
    {
        public int ID { get; set; }
        public string? AllocationNo { get; set; }
        public int AssetID { get; set; }
        public DateTime AllocationDate { get; set; }

        /// <summary>Branch | Staff | Guard</summary>
        public string AllocatedToType { get; set; } = string.Empty;

        public int? AllocatedToID { get; set; }
        public string? AllocatedToName { get; set; }
        public string? BranchCode { get; set; }
        public string? Remarks { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    public class AssetAllocationListDto
    {
        public int ID { get; set; }
        public string AllocationNo { get; set; } = string.Empty;
        public int AssetID { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string? AssetName { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime AllocationDate { get; set; }
        public string AllocatedToType { get; set; } = string.Empty;
        public int? AllocatedToID { get; set; }
        public string? AllocatedToName { get; set; }
        public string? BranchCode { get; set; }
        public string? Remarks { get; set; }
        public bool IsActive { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
