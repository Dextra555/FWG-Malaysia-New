namespace OBMS.WebAPI.Models.DTO
{
    public class ChequeMasterDto
    {
        public int ID { get; set; }
        public int BankID { get; set; }
        public string? AccountName { get; set; }
        public int ChequeStart { get; set; }
        public int ChequeEnd { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
