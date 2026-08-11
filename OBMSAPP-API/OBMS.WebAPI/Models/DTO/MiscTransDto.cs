namespace OBMS.WebAPI.Models.DTO
{
    public class MiscTransDto
    {
        public int ID { get; set; }
        public DateTime TransDate { get; set; }
        public int EmployeeID { get; set; }
        public int TransType { get; set; }
        public decimal Amount { get; set; }
        public string? Particulars { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
