namespace OBMS.WebAPI.Models.DTO
{
    public class EPFRequestDto
    {
        public int? epf_id { get; set; }
        public decimal? epf_from { get; set; }
        public decimal? epf_to { get; set; }
        public decimal? epf_worker { get; set; }
        public decimal? epf_worker8Pa { get; set; }
        public decimal? epf_boss { get; set; }
        public decimal? epf_total { get; set; }
        public decimal? epf_total8Pa { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
        public decimal? epf_worker55 { get; set; }
        public decimal? epf_boss55 { get; set; }
        public decimal? epf_total55 { get; set; }
        public decimal? epf_tatal7pa { get; set; }
        public decimal? epf_worker7pa { get; set; }
        public decimal? epf_total7pa { get; set; }
        public decimal? epf_worker9pa { get; set; }
    }
}
