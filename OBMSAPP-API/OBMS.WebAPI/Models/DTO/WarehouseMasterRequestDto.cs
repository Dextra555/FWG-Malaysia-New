namespace OBMS.WebAPI.Models.DTO
{
    public class WarehouseMasterRequestDto
    {
        public int ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public int BranchID { get; set; }
        public bool IsActive { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
