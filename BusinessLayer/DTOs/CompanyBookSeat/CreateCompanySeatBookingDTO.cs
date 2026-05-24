using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class CreateCompanySeatBookingDTO
    {
        [Required]
        public Guid CompanyId { get; set; }

        public CompanySeatBookingDirection BookingDirection { get; set; }
            = CompanySeatBookingDirection.Inbound;

        // ── Inbound fields ────────────────────────────────────────────
        public Guid? TripId { get; set; }
        public List<Guid> SeatIds { get; set; } = new();

        // ── Outbound fields ───────────────────────────────────────────
        public int SeatsCount { get; set; } = 1;
        public DateTime? TripDate { get; set; }

        // ── مشتركة ────────────────────────────────────────────────────
        [MaxLength(200)]
        public string FromLocation { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ToLocation { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal PricePerSeat { get; set; } = 0;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // ── بيانات العميل (اختيارية — لما الشركة بتحجز لعميل معين) ───
        [MaxLength(200)]
        public string? ClientName { get; set; }

        [MaxLength(20)]
        public string? ClientPhone { get; set; }

        public TransportationTripType ClientTripType { get; set; }
            = TransportationTripType.Departure;
    }
}
