using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Book
{
    public sealed class UpdateBookingDTO : CreateBookingDTO
    {
        [Required]
        public Guid BookingID { get; set; }

        public string Code { get; set; } = string.Empty;
    }
}
