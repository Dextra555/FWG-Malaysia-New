using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("InventoryCategory")]
    public class InventoryCategory
    {
        [Key]
        public int ID { get; set; }
        public string Name { get; set; }
        public string Cat { get; set; }
        public string? AssetType { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
