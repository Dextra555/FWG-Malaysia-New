namespace OBMS.WebAPI.Models.DTO
{
    public class SIPRequestDto
    {
        public int SIP_id { get; set; }
        public decimal SIP_from { get; set; }
        public decimal SIP_to { get; set; }
        public decimal SIP_worker { get; set; }
        public decimal SIP_boss { get; set; }
        public decimal SIP_total { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
