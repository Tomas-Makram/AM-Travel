using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class AddCompanySeatPaymentDTO
    {
        [Required]
        public Guid CompanyId { get; set; }

        public Guid? TripId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.Today;

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
