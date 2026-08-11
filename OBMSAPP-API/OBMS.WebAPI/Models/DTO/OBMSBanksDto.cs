namespace OBMS.WebAPI.Models.DTO
{
    public class OBMSBanksDto
    {
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? BankCode { get; set; }
        public int BankID { get; set; }
        public bool? IsAllowed { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
