using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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
        public int? ParentCategoryID { get; set; }
        public string? LastUpdatedBy { get; set; }

        [JsonIgnore]
        [ForeignKey("ParentCategoryID")]
        public virtual InventoryCategory? ParentCategory { get; set; }
    }
}
