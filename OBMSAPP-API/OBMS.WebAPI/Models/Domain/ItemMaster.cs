using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ItemMaster")]
    public class ItemMaster
    {
        [Key]
        public int ID { get; set; }
        public int CategoryID { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public decimal? SellPrice { get; set; }
        public string? Remarks { get; set; }
        public decimal? Quantity { get; set; }
    }
}
