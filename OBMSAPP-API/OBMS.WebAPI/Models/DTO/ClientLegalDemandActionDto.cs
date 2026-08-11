namespace OBMS.WebAPI.Models.DTO
{
    public class ClientLegalDemandActionDto
    {
        public int ID { get; set; }
        public string? BranchCode { get; set; }
        public string? BranchName { get; set; }
        public string? ClientCode { get; set; }
        public string? ClientName { get; set; }
        public string? ActionTaken { get; set; }
        public DateTime DateIssue { get; set; }
        public string? Remarks { get; set; }
        public string? DeletionRemarks { get; set; }
        public bool IsDeleted { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

}
