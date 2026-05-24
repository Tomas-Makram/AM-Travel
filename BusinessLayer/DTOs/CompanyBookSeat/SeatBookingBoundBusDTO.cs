using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class SeatBookingBoundBusDTO
    {
        public Guid BusId { get; set; }
        public Guid? TripId { get; set; }

        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public DateTime? TripDate { get; set; }

        public TripDirection Direction { get; set; } = TripDirection.Departure;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public int TotalSeats { get; set; }
        public int ClientReserved { get; set; }
        public int CompanyReserved { get; set; }
        public int AvailableSeats { get; set; }

        public bool HasTrip { get; set; }
        public bool IsFull { get; set; }

        public bool IsFallbackOption { get; set; }
    }
}
