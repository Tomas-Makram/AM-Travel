using System.ComponentModel.DataAnnotations;
using DataLayer.Models;

namespace BusinessLayer.DTOs.Book
{
    public sealed class AddBookingPaymentDTO
    {
        [Required]
        public Guid BookingId { get; set; }

        [Range(1, 999999999, ErrorMessage = "Payment amount must be greater than zero.")]
        public decimal Amount { get; set; }

        public PayType PayType { get; set; } = PayType.cache;

        [DataType(DataType.Date)]
        public DateTime PaidAt { get; set; } = DateTime.Today;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}