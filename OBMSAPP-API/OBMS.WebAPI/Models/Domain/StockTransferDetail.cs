using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Line item for a Stock Transfer document.
    /// Each line creates one TRANSFER_OUT + one TRANSFER_IN row in StockLedger.
    /// TransferQty must not exceed available stock in the source warehouse.
    /// </summary>
    [Table("StockTransferDetail")]
    public class StockTransferDetail
    {
        [Key]
        public int ID { get; set; }

        public int TransferID { get; set; }

        public int ItemID { get; set; }

        public int? UOMID { get; set; }

        public decimal TransferQty { get; set; }

        public decimal UnitCost { get; set; } = 0;

        public string? Remarks { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("TransferID")]
        public virtual StockTransfer? Transfer { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }

        [ForeignKey("UOMID")]
        public virtual UOMMaster? UOM { get; set; }
    }
}
