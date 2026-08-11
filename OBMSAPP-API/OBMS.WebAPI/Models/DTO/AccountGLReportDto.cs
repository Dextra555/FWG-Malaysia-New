namespace OBMS.WebAPI.Models.DTO
{
    public class AccountGLReportDto
    {
        public int ProcessYear { get; set; }
        public string Branch { get; set; }
        public DateTime IssueDate { get; set; }
        public string RefNo { get; set; }
        public string ChequeNo { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public string Type { get; set; }
        public string FromTable { get; set; }
        public string Category { get; set; }
        public string CreatedBy { get; set; }
    }
}
