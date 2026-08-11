using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OBMS.WebAPI.Models.Domain
{
    [Table("ChequeStatus")]
    public class ChequeStatus
    {
        [Key]
        public int ID { get; set; }
        public string TransType { get; set; }
        public DateTime ChequeDate { get; set; }
        public string BankCode { get; set; }
        public string ChequeNo { get; set; }
        public decimal ChequeAmount { get; set; }

        [Column("ChequeStatus")]
        public string? Status { get; set; }
        public string Particulars { get; set; }
        //public bool IsDeleted { get; set; }
    }
}
