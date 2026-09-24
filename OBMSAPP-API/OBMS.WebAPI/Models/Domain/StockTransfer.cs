using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    /// <summary>
    /// Header record for a Warehouse-to-Warehouse Stock Transfer.
    /// Each save creates paired TRANSFER_OUT (source) + TRANSFER_IN (destination) 
    /// rows in StockLedger within a single transaction.
    /// Source and destination warehouses must be different.
    /// </summary>
    [Table("StockTransfer")]
    public class StockTransfer
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated reference number, e.g. TRF0001</summary>
        public string TransferNo { get; set; } = string.Empty;

        public DateTime TransferDate { get; set; }

        public int SourceWarehouseID { get; set; }

        public int DestWarehouseID { get; set; }

        public string? Remarks { get; set; }

        /// <summary>Soft-delete flag.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("SourceWarehouseID")]
        public virtual WarehouseMaster? SourceWarehouse { get; set; }

        [ForeignKey("DestWarehouseID")]
        public virtual WarehouseMaster? DestWarehouse { get; set; }

        public virtual ICollection<StockTransferDetail> Details { get; set; } = new List<StockTransferDetail>();
    }
}
