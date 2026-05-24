using BusinessLayer.DTOs.Company;
using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class TransferTripDetailsDTO
    {
        public Guid TripId { get; set; }
        public DateTime TripDate { get; set; }
        public TripDirection Direction { get; set; }
        public string DirectionText => Direction == TripDirection.Return ? "عودة" : "ذهاب";

        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public List<TransferTripPassengerSeatDTO> PassengerSeats { get; set; } = new();
        public List<TransferTripCompanySeatDTO> CompanySeats { get; set; } = new();

        public int BookingsCount => PassengerSeats.Select(x => x.BookingId).Distinct().Count();
        public int SeatsCount => PassengerSeats.Count;
        public int CompanySeatsCount => CompanySeats.Count;
    }
}
