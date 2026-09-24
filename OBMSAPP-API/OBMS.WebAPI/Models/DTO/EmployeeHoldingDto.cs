namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// Summary of items currently held by a single Employee or Guard.
    /// Computed as: TotalIssued - TotalReturned per item per employee.
    /// Source: StockIssueDoc + StockIssueDocDetail + StockIssueReturn + StockIssueReturnDetail
    /// </summary>
    public class EmployeeHoldingDto
    {
        /// <summary>EMP_ID from Employee table — stored as IssueToID in StockIssueDoc</summary>
        public int? EmployeeID { get; set; }

        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>Employee | Guard</summary>
        public string IssueToType { get; set; } = string.Empty;

        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UOMName { get; set; } = string.Empty;

        public decimal TotalIssued { get; set; }
        public decimal TotalReturned { get; set; }

        /// <summary>TotalIssued - TotalReturned — what the employee currently holds</summary>
        public decimal NetHolding { get; set; }

        public decimal UnitCost { get; set; }

        /// <summary>NetHolding × UnitCost</summary>
        public decimal TotalValue { get; set; }

        /// <summary>Date of the most recent issue for this item to this employee</summary>
        public DateTime? LastIssueDate { get; set; }

        /// <summary>Reference number of the most recent issue document</summary>
        public string? LastIssueNo { get; set; }
    }

    /// <summary>
    /// Summary row per employee — total items held and total value.
    /// Used in the employee list before drilling into per-item detail.
    /// </summary>
    public class EmployeeHoldingSummaryDto
    {
        public int? EmployeeID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string IssueToType { get; set; } = string.Empty;

        /// <summary>Number of distinct items currently held</summary>
        public int ItemCount { get; set; }

        /// <summary>Total net holding value across all items</summary>
        public decimal TotalValue { get; set; }

        /// <summary>Date of last issue to this employee</summary>
        public DateTime? LastIssueDate { get; set; }
    }
}
