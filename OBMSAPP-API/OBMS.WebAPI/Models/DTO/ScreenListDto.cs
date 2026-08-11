namespace OBMS.WebAPI.Models.DTO
{
    public class ScreenListDto
    {
        public string? ScreenName { get; set; }
        public string? Category { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
