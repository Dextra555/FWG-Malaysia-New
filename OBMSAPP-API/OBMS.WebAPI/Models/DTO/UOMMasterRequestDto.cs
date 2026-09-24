namespace OBMS.WebAPI.Models.DTO
{
    public class UOMMasterRequestDto
    {
        public int ID { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? ShortName { get; set; }
        public bool IsPrimary { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
