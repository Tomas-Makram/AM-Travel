using BusinessLayer.Attributes;
using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Work
{
    public class CreateWorkDTO
    {
        [Required]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^01[0-2,5][0-9]{8}$", ErrorMessage = "Phone number is incorrect.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Client Name")]
        public string NameClient { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Client Type")]
        public ClientType ClientType { get; set; } = ClientType.threeStar;

        [Display(Name = "Notes")]
        public string Notes { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Next Action Date")]
        [FutureDate(ErrorMessage = "Next action date cannot be in the past.")]
        public DateTime DayUpdated { get; set; } = DateTime.Today;
    }
}