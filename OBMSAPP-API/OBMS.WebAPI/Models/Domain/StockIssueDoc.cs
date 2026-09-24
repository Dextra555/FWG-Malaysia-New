using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Header record for a Stock Issue (Stock Out) transaction.
    /// Stock is issued from a warehouse to an Employee, Guard, Branch, or Other party.
    /// Each save creates ISSUE rows in StockLedger, reducing available stock.
    /// </summary>
    [Table("StockIssueDoc")]
    public class StockIssueDoc
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated reference number, e.g. SI0001</summary>
        public string IssueNo { get; set; } = string.Empty;

        public DateTime IssueDate { get; set; }

        public int WarehouseID { get; set; }

        /// <summary>Who the stock is being issued to: Employee | Guard | Branch | Other</summary>
        public string IssueToType { get; set; } = string.Empty;

        /// <summary>FK to Employee/Branch table (nullable — not used for "Other")</summary>
        public int? IssueToID { get; set; }

        /// <summary>Display name of the recipient (auto-filled from lookup, or free-text for Other)</summary>
        public string? IssueToName { get; set; }

        public string? Remarks { get; set; }

        /// <summary>Soft-delete flag.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("WarehouseID")]
        public virtual WarehouseMaster? Warehouse { get; set; }

        public virtual ICollection<StockIssueDocDetail> Details { get; set; } = new List<StockIssueDocDetail>();
    }
}
