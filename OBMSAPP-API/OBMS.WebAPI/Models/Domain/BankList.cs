using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("BankList")]    
    public class BankList
    {
        [Key]
        public int ID { get; set; }

        [StringLength(10)]
        public string? BankCode { get; set; }

        [StringLength(255)]
        public string? BankName { get; set;}

        public DateTime LASTUPDATE { get; set;}

        [StringLength(20)]
        public string? LastUpdatedBy { get; set;}
    }
}
