namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// One Stock Issue document header, with its line items,
    /// returned when the user clicks "Link from Stock Issue" in the Uniform Loan page.
    /// Only issues NOT already linked to an active Uniform Loan are returned.
    /// </summary>
    public class PendingStockIssueDto
    {
        public int IssueDocID { get; set; }
        public string IssueNo { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public List<PendingStockIssueLineDto> Lines { get; set; } = new();
    }

    public class PendingStockIssueLineDto
    {
        public int IssueDetailID { get; set; }
        public int ItemID { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public decimal IssueQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// Payload sent from Angular when creating a Uniform Loan linked to a Stock Issue.
    /// </summary>
    public class LinkStockIssueToLoanRequestDto
    {
        /// <summary>StockIssueDoc.ID being linked</summary>
        public int StockIssueDocID { get; set; }

        /// <summary>SalaryAdvance.ID (TransType=4) just created/updated by the loan save</summary>
        public int SalaryAdvanceID { get; set; }

        public string? LinkedBy { get; set; }
    }
}
