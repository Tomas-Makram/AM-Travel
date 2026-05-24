using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Account
{
    public class ChangeFieldsDTO
    {
        [Required]
        public Guid UserId { get; set; } = Guid.NewGuid();

        [Required]
        [MinLength(3)]
        [MaxLength(25)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [RegularExpression(@"^(01)[0-2,5]{1}[0-9]{8}$", ErrorMessage = "Phone number is not valid.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be exactly 14 digits.")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "National ID must contain exactly 14 digits.")]
        public string NationalId { get; set; } = string.Empty;
    }
}