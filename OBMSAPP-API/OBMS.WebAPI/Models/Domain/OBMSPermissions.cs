using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("OBMSPermissions")]
    public class OBMSPermissions
    {
        [Key]
        public int ID { get; set; }
        public string? Name { get; set; }
        public string? ScreenName { get; set; }
        public bool? Create { get; set; }
        public bool? Read { get; set; }
        public bool? Update { get; set; }
        public bool? Delete { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
