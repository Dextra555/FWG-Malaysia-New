using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ClientBreak")]
    public class ClientMasterBreak
    {
        [Key]
        public int ID { get; set; }

        public string ClientCode { get; set; }

        public decimal BreakHours { get; set; }

        public int SortOrder { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
