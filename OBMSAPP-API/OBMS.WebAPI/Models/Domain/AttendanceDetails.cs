using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("AttendanceDetails")]
    public class AttendanceDetails
    {
        [Key]
        public int ID { get; set; }
        public int AttendanceID { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string? Client { get; set; }
        public DateTime? TimeStart { get; set; }
        public DateTime? TimeEnd { get; set; }
        public string? OTClient { get; set; }
        public DateTime? OTTimeStart { get; set; }
        public DateTime? OTTimeEnd { get; set; }
        public int Type { get; set; }
        public DateTime LastUpdate { get; set; }
        public string? LastUpdatedBy { get; set; }
    }
}
