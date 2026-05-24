using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Account
{
    public class CreateNewAccountDTO
    {

        [Required]
        [MinLength(3)]
        [MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MinLength(3)]
        [MaxLength(25)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [RegularExpression(@"^(01)[0-2,5]{1}[0-9]{8}$", ErrorMessage = "Phone number is not valid.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).+$",ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password), ErrorMessage = "Password confirmation does not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(14, MinimumLength = 14, ErrorMessage = "National ID must be exactly 14 digits.")]
        [RegularExpression(@"^\d{14}$", ErrorMessage = "National ID must contain exactly 14 digits.")]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(AccountType), ErrorMessage = "Invalid account type.")]
        public AccountType AccountType { get; set; } = AccountType.Viewer;
    }
}
