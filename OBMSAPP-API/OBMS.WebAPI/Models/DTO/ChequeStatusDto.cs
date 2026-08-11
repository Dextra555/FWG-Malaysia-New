namespace OBMS.WebAPI.Models.DTO
{
    public class ChequeStatusDto
    {
        public int ID { get; set; }
        public string TransType { get; set; }
        public DateTime ChequeDate { get; set; }
        public string BankCode { get; set; }
        public string ChequeNo { get; set; }
        public decimal ChequeAmount { get; set; }
        public string ChequeStatus { get; set; }
        public string Particulars { get; set; }
    }

}
