using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Header record for a Stock Issue Return.
    /// Previously issued stock is returned to the warehouse.
    /// Each return creates ISSUE_RETURN rows in StockLedger, increasing available stock.
    /// </summary>
    [Table("StockIssueReturn")]
    public class StockIssueReturn
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated reference number, e.g. SIR0001</summary>
        public string ReturnNo { get; set; } = string.Empty;

        public DateTime ReturnDate { get; set; }

        /// <summary>FK to the original StockIssueDoc being returned</summary>
        public int OriginalIssueDocID { get; set; }

        /// <summary>Warehouse where the stock is being returned to</summary>
        public int WarehouseID { get; set; }

        public string? Remarks { get; set; }

        /// <summary>Soft-delete flag.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("OriginalIssueDocID")]
        public virtual StockIssueDoc? OriginalIssueDoc { get; set; }

        [ForeignKey("WarehouseID")]
        public virtual WarehouseMaster? Warehouse { get; set; }

        public virtual ICollection<StockIssueReturnDetail> Details { get; set; } = new List<StockIssueReturnDetail>();
    }
}
