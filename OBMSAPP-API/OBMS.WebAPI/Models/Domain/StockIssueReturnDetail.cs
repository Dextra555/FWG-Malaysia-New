using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Line item for a Stock Issue Return.
    /// Each line creates one ISSUE_RETURN row in StockLedger.
    /// ReturnQty is validated against (original IssueQty - already returned qty).
    /// </summary>
    [Table("StockIssueReturnDetail")]
    public class StockIssueReturnDetail
    {
        [Key]
        public int ID { get; set; }

        public int ReturnID { get; set; }

        /// <summary>FK to the specific issue detail line being returned</summary>
        public int OriginalIssueDetailID { get; set; }

        public int ItemID { get; set; }

        public int? UOMID { get; set; }

        public decimal ReturnQty { get; set; }

        public decimal UnitCost { get; set; } = 0;

        public string? Remarks { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("ReturnID")]
        public virtual StockIssueReturn? Return { get; set; }

        [ForeignKey("OriginalIssueDetailID")]
        public virtual StockIssueDocDetail? OriginalIssueDetail { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }

        [ForeignKey("UOMID")]
        public virtual UOMMaster? UOM { get; set; }
    }
}
