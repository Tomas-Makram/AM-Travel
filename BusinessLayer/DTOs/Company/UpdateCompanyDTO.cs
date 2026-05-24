using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Company
{
    public class UpdateCompanyDTO
    {
        [Required]
        public Guid CompanyId { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [MaxLength(160, ErrorMessage = "Company name cannot exceed 160 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [MinLength(11, ErrorMessage = "Phone number must be 11 digits")]
        [MaxLength(11, ErrorMessage = "Phone number must be 11 digits")]
        [RegularExpression(@"^01[0-2,5][0-9]{8}$",
            ErrorMessage = "Enter a valid Egyptian phone number مثل 01012345678")]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}