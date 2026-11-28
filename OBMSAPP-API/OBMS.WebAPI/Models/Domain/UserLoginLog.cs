using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace OBMS.WebAPI.Models.Domain;

[Table("OBMSUserLoginLog")]
public class UserLoginLog
{
    [Key]
    public int ID { get; set; }

    [Required]
    [StringLength(200)]
    public string UserName { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime LoginTime { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LogoutTime { get; set; }

    public bool IsActive { get; set; }
}
