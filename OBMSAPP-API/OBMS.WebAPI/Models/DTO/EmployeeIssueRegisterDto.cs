namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// One row in the Employee/Guard Issue Register report.
    /// Shows every issue line within the selected date range
    /// for Employee/Guard-type issue documents.
    /// </summary>
    public class EmployeeIssueRegisterDto
    {
        public string IssueNo { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string IssueToType { get; set; } = string.Empty;
        public int? EmployeeID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;

        public int ItemID { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string UOMName { get; set; } = string.Empty;

        public decimal IssueQty { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalValue { get; set; }

        /// <summary>How much of this line has been returned so far</summary>
        public decimal ReturnedQty { get; set; }

        /// <summary>IssueQty - ReturnedQty</summary>
        public decimal NetQty { get; set; }

        public string? Remarks { get; set; }
        public string? IssuedBy { get; set; }
    }
}
