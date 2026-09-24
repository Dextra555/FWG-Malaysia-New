using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("UOMMaster")]
    public class UOMMaster
    {
        [Key]
        public int ID { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public string? ShortName { get; set; }

        public bool IsPrimary { get; set; }

        public DateTime LASTUPDATE { get; set; }

        public string? LastUpdatedBy { get; set; }
    }
}