using DataLayer.Models;

namespace BusinessLayer.DTOs.Trip
{
    public class AvailableTripListDTO
    {
        public Guid TripId { get; set; }
        public Guid BusId { get; set; }
        public Guid? CompanyId { get; set; }

        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public TripDirection Direction { get; set; }
        public string DirectionText { get; set; } = string.Empty;

        public DateTime TripDate { get; set; }

        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public int TotalSeats { get; set; }
        public int ReservedSeats { get; set; }
        public int AvailableSeats { get; set; }

        public decimal CompanyTripPrice { get; set; }
        public decimal CompanyTripPaidAmount { get; set; }
        public decimal CompanyTripRemainingAmount { get; set; }

        public string CompanyName { get; set; } = string.Empty;
        public string CompanyPhoneNumber { get; set; } = string.Empty;
        public Guid? CompanyTripGroupId { get; set; }
    }
}
