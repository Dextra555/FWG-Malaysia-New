namespace OBMS.WebAPI.Models.DTO
{
    /// <summary>
    /// DTO used by Payroll module for Employee Item Issue (uniform/equipment issue).
    /// Separated from ItemMasterDto to avoid coupling Payroll and Inventory concerns.
    /// </summary>
    public class EmployeeItemIssueDto
    {
        public int ID { get; set; }           // EmployeeItemIssue.ID (0 if not yet issued)
        public int AdvanceID { get; set; }    // FK to SalaryAdvance
        public int ItemID { get; set; }       // FK to ItemMaster
        public string? Name { get; set; }     // ItemMaster.Name
        public decimal Price { get; set; }    // ItemMaster.SellPrice (historical — payroll uses sell price for uniform cost)
        public int Quantity { get; set; }     // EmployeeItemIssue.Quantity (0 if not yet issued)
        public string? LastUpdatedBy { get; set; }
    }
}
