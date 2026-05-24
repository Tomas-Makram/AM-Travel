using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Book
{
    public sealed class BookingPhoneDTO
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^01[0-2,5][0-9]{8}$", ErrorMessage = "Phone number is incorrect.")]
        public string PhoneNumber { get; set; } = string.Empty;

        public bool Prime { get; set; }
    }
}