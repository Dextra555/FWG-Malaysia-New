namespace OBMS.WebAPI.Models.DTO
{
    public class ShiftTimeMasterDto
    {
        public int ID { get; set; }
        public string? ShiftType { get; set; }
        public string? ShiftFrom { get; set; }
        public string? ShiftTo { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
