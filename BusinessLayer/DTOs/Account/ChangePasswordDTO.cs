using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Account
{
    public class ChangePasswordDTO
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        public string ConfirmPassword { get; set; }= string.Empty;
    }
}
