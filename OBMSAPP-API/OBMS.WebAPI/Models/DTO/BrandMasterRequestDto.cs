namespace OBMS.WebAPI.Models.DTO
{
    public class BrandMasterRequestDto
    {
        public int ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
