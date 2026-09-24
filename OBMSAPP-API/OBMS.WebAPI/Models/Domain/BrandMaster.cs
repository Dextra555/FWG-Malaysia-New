using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("BrandMaster")]
    public class BrandMaster
    {
        [Key]
        public int ID { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }
    }
}