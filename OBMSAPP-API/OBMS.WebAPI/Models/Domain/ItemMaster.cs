using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ItemMaster")]
    public class ItemMaster
    {
        [Key]
        public int ID { get; set; }
        public string? Code { get; set; }
        public int CategoryID { get; set; }
        public int? BrandID { get; set; }
        public int? UOMID { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public decimal? SellPrice { get; set; }       // Retained for DB compatibility — not exposed in UI
        public string? Remarks { get; set; }
        public decimal? Quantity { get; set; }         // Retained for DB compatibility — not exposed as editable in UI
        public decimal? ReorderLevel { get; set; }
        public decimal? ReorderQuantity { get; set; }

        [ForeignKey("BrandID")]
        public virtual BrandMaster? Brand { get; set; }

        [ForeignKey("UOMID")]
        public virtual UOMMaster? UOM { get; set; }
    }
}
