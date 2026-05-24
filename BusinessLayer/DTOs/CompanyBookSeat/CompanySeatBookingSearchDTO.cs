using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class CompanySeatBookingSearchDTO
    {
        public CompanySeatBookingDirection BookingDirection;

        public Guid? CompanyId { get; set; }
        public Guid? TripId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Location { get; set; }
        public CompanySeatBookingDirection? Direction { get; set; }
        public CompanySeatPaymentStatus PaymentStatus { get; set; } = CompanySeatPaymentStatus.All;
    }
}
