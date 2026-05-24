using BusinessLayer.DTOs.Company;
using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class CompanySeatAccountPageDTO
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;
        public string? CompanyNotes { get; set; }

        public List<CompanyTripSeatSummaryDTO> TripGroups { get; set; } = new();
        public List<CompanySeatPaymentDTO> Payments { get; set; } = new();

        public decimal GrandTotalPrice => TripGroups.Sum(x => x.TotalPrice);
        public decimal GrandTotalPaid => Payments.Sum(x => x.Amount);
        public decimal GrandTotalRemaining => Math.Max(0, GrandTotalPrice - GrandTotalPaid);

        public decimal InboundTotal => TripGroups
            .Where(x => x.BookingDirection == CompanySeatBookingDirection.Inbound)
            .Sum(x => x.TotalPrice);

        public decimal OutboundTotal => TripGroups
            .Where(x => x.BookingDirection == CompanySeatBookingDirection.Outbound)
            .Sum(x => x.TotalPrice);

        public decimal NetBalance => InboundTotal - OutboundTotal;
    }
}
