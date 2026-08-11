using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("LeaveSystem")]
    public class LeaveSystem
    {
        [Key]
        public int LS_ID { get; set; }
        public decimal al0to1 { get; set; }
        public decimal AL1to2 { get; set; }
        public decimal AL2to5 { get; set; }
        public decimal AL6 { get; set; }
        public decimal ml0to2 { get; set; }
        public decimal ml2to5 { get; set; }
        public decimal ML6 { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public string? LastUpdatedBy { get; set; }
        public decimal HL { get; set; }
        public decimal MtnyL { get; set; }
        public decimal PtnyL { get; set; }
    }
}
