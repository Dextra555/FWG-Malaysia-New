namespace OBMS.WebAPI.Models.DTO
{
    public class TerminatedAgreementRequestDto
    {
        public int ID { get; set; }
        public string? Branch { get; set; }
        public string? Client { get; set; }
        public string? Reason { get; set; }
        public DateTime TerminationDate { get; set; }
        public string? Note { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
