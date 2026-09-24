using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("OpeningStock")]
    public class OpeningStock
    {
        [Key]
        public int ID { get; set; }

        /// <summary>Auto-generated reference number, e.g. OS0001</summary>
        public string ReferenceNo { get; set; } = string.Empty;

        public int WarehouseID { get; set; }

        public int ItemID { get; set; }

        public DateTime OpeningDate { get; set; }

        public decimal OpeningQty { get; set; }

        /// <summary>Cost per unit at the time of opening stock entry</summary>
        public decimal UnitCost { get; set; }

        /// <summary>Stored value = OpeningQty * UnitCost (calculated by API before save)</summary>
        public decimal TotalValue { get; set; }

        public string? Remarks { get; set; }

        /// <summary>Soft-delete flag. False = record deleted; does not remove from DB.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("WarehouseID")]
        public virtual WarehouseMaster? Warehouse { get; set; }

        [ForeignKey("ItemID")]
        public virtual ItemMaster? Item { get; set; }
    }
}
