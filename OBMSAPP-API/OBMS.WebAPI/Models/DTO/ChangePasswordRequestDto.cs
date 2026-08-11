using System.ComponentModel.DataAnnotations;

namespace OBMS.WebAPI.Models.DTO
{
    public class ChangePasswordRequestDto
    {
        [Required]
        public string CurrentUser { get; set; }

        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        public string NewPassword { get; set; }
    }
}
