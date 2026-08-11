namespace OBMS.WebAPI.Models.DTO
{
    public class OBMSPermissionDto
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? ScreenName { get; set; }
        public bool? Create { get; set; }
        public bool? Read { get; set; }
        public bool? Update { get; set; }
        public bool? Delete { get; set; }
        public string? LastUpdatedBy { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? Category { get; set; }
    }
}
