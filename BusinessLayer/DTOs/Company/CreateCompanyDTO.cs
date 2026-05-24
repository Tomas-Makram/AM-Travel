using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Company
{
    public class CreateCompanyDTO
    {
        [Required, MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [MaxLength(11, ErrorMessage = "Phone number must be 11 digits")]
        [MinLength(11, ErrorMessage = "Phone number must be 11 digits")]
        [RegularExpression(@"^01[0-2,5]{1}[0-9]{8}$", ErrorMessage = "Enter a valid Egyptian phone number (e.g. 01012345678)")]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

    }
}
