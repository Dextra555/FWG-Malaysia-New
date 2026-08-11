namespace OBMS.WebAPI.Models.DTO
{
    public class SOCSORequestDto
    {
        public int? socso_id { get; set; }
        public decimal? socso_from { get; set; }
        public decimal? socso_to { get; set; }
        public decimal? socso_employer { get; set; }
        public decimal? socso_worker { get; set; }
        public decimal? socso_total { get; set; }
        public decimal? socso_50year { get; set; }
        public decimal? socso_foreigner { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
