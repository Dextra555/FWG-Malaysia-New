namespace OBMS.WebAPI.Models.DTO
{
    public class BankMasterDto
    {
        public int BankId { get; set; }
        public string BankCode { get; set; }
        public string Accname { get; set; }
        public string Accno { get; set; }
        public string PREFIX { get; set; }
        public string AccShortName { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string LastUpdatedBy { get; set; }
    }
}
