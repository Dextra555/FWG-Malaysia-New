namespace OBMS.WebAPI.Models.DTO
{    
    public class OBMSBranchesDto
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? BranchCode { get; set; }
        public bool? IsAllowed { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
