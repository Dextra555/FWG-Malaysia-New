using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Line item for a Stock Issue document.
    /// Each line creates one ISSUE row in StockLedger.
    /// </summary>
    [Table("StockIssueDocDetail")]
    public class StockIssueDocDetail
    {
        [Key]
        public int ID { get; set; }

        public int IssueDocID { get; set; }

        public int ItemID { get; set; }

        public int? UOMID { get; set; }

        public decimal IssueQty { get; set; }

        public decimal UnitCost { get; set; } = 0;

        public string? Remarks { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("IssueDocID")]
        public virtual StockIssueDoc? IssueDoc { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }

        [ForeignKey("UOMID")]
        public virtual UOMMaster? UOM { get; set; }
    }
}
