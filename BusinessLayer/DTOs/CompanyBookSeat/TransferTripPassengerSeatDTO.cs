using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class TransferTripPassengerSeatDTO
    {
        public Guid BookingSeatId { get; set; }
        public Guid BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;
        public string? ClientPhone { get; set; }

        public Guid TripId { get; set; }
        public DateTime TripDate { get; set; }
        public TripDirection Direction { get; set; }
        public string DirectionText => Direction == TripDirection.Return ? "عودة" : "ذهاب";

        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public Guid SeatId { get; set; }
        public string SeatLabel { get; set; } = string.Empty;

        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public string RouteText => $"{FromLocation} → {ToLocation}";

        public decimal SeatPrice { get; set; }

        public bool AlreadyTransferred { get; set; }
        public string? TransferredToCompany { get; set; }
    }
}
