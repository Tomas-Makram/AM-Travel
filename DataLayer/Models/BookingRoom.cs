using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class BookingRoom
    {
        [Key]
        public Guid BookingRoomId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public BookingData Booking { get; set; } = null!;

        [Required]
        public RoomType RoomType { get; set; }

        [Range(1, 100)]
        public int Count { get; set; } = 1;

        [Range(0, 999999999)]
        public decimal NightPrice { get; set; }

        public decimal Total => Count * NightPrice;
    }
}