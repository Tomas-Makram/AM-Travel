using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class CompanyTripSeatSummaryDTO
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;

        public Guid? TripId { get; set; }
        public DateTime TripDate { get; set; }
        public TripDirection? TripDirection { get; set; }
        public string TripFromLocation { get; set; } = string.Empty;
        public string TripToLocation { get; set; } = string.Empty;
        public Guid? BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public CompanySeatBookingDirection BookingDirection { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public List<CompanySeatBookingItemDTO> Seats { get; set; } = new();

        public int SeatsCount => Seats.Sum(x => x.SeatsCount);
        public decimal PricePerSeat { get; set; }
        public decimal TotalPrice => Seats.Sum(x => x.TotalPrice);
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining => Math.Max(0, TotalPrice - TotalPaid);
    }
}
