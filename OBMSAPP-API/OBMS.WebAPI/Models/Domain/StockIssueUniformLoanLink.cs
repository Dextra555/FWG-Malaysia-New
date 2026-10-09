using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Tracks which StockIssueDoc has been linked to a SalaryAdvance (TransType=4, Uniform Loan).
    /// IsActive=true means an active link exists — prevents a Stock Issue from being linked to
    /// more than one active Uniform Loan.
    /// </summary>
    [Table("StockIssueUniformLoanLink")]
    public class StockIssueUniformLoanLink
    {
        [Key]
        public int ID { get; set; }

        /// <summary>FK → StockIssueDoc.ID</summary>
        public int StockIssueDocID { get; set; }

        /// <summary>FK → SalaryAdvance.ID  (TransType = 4)</summary>
        public int SalaryAdvanceID { get; set; }

        public DateTime LinkedDate { get; set; } = DateTime.Now;

        public string? LinkedBy { get; set; }

        /// <summary>true while the loan is active. Set false when loan is deleted.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation
        [ForeignKey("StockIssueDocID")]
        public virtual StockIssueDoc? StockIssueDoc { get; set; }

        [ForeignKey("SalaryAdvanceID")]
        public virtual SalaryAdvance? SalaryAdvance { get; set; }
    }
}
