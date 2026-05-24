using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Work
{
    public class UpdateWorkDTO
    {
        [Required]
        public Guid WorkId { get; set; }

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
        public DateTime DayUpdated { get; set; } = DateTime.Today;
    }
}