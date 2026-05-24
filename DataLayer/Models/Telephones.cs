using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class Telephones
    {
        [Key]
        public Guid Id { get; set; } = Guid.Empty;

        [Required]
        [RegularExpression(@"^01[0-2,5][0-9]{8}$", ErrorMessage = "Phone Number is incorrect")]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool Prime { get; set; } = false;

        public Guid BookingID { get; set; }

        [ForeignKey(nameof(BookingID))]
        public virtual BookingData Booking { get; set; } = null!;
    }
}
