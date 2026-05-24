using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class BookingPayment
    {
        [Key]
        public Guid PaymentId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public BookingData Booking { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PayType PayType { get; set; } = PayType.cache;

        public DateTime PaidAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}