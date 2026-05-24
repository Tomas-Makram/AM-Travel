using DataLayer.Models;

namespace BusinessLayer.DTOs.Book
{
    public sealed class TransportationBookingDTO
    {
        public TransportationTripType TripType { get; set; } = TransportationTripType.Departure;

        public int DepartureRequiredSeats { get; set; } = 1;
        public int ReturnRequiredSeats { get; set; } = 1;

        public DateTime? DepartureDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        public Guid? DepartureBusId { get; set; }
        public Guid? ReturnBusId { get; set; }

        public Guid? DepartureTripId { get; set; }
        public Guid? ReturnTripId { get; set; }

        public bool CreateNewDepartureTrip { get; set; } = false;
        public bool CreateNewReturnTrip { get; set; } = false;

        public string? DepartureFromLocation { get; set; }
        public string? DepartureToLocation { get; set; }

        public string? ReturnFromLocation { get; set; }
        public string? ReturnToLocation { get; set; }

        public List<BookingSeatDTO> DepartureSeats { get; set; } = new();
        public List<BookingSeatDTO> ReturnSeats { get; set; } = new();
    }
}