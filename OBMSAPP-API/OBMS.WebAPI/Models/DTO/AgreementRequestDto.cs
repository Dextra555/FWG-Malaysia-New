using System.ComponentModel.DataAnnotations;

namespace OBMS.WebAPI.Models.DTO
{
    public class AgreementRequestDto
    {
        public int ID { get; set; }
        public string Branch { get; set; }
        public string Client { get; set; }
        public string WorkPlace { get; set; }
        public DateTime AgreementDate { get; set; }
        public DateTime AgreementEndDate { get; set; }
        public string Note { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool? IsValid { get; set; }
        public AgreementDetailsRequestDto[]? details { get; set; }

    }
}
