using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Book
{
    public sealed class BookingPaymentDTO
    {
        public Guid? PaymentId { get; set; }

        [Range(0, 999999999, ErrorMessage = "Payment amount cannot be negative.")]
        public decimal Amount { get; set; }

        public PayType PayType { get; set; } = PayType.cache;

        [DataType(DataType.Date)]
        public DateTime PaidAt { get; set; } = DateTime.Today;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
