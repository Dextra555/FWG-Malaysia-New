using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ShiftTimeMaster")]
    public class ShiftTimeMaster
    {
        [Key]
        public int ID { get; set; }
        public string ShiftType { get; set; } = null!;
        public string? ShiftFrom { get; set; }
        public string? ShiftTo { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
