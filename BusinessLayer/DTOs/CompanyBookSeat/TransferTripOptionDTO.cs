using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class TransferTripOptionDTO
    {
        public Guid TripId { get; set; }
        public Guid BusId { get; set; }
        public DateTime TripDate { get; set; }
        public TripDirection Direction { get; set; }

        public string DirectionText => Direction == TripDirection.Return ? "Return" : "Go";
        public string DirectionTextAr => Direction == TripDirection.Return ? "عودة" : "ذهاب";

        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public int ClientBookingsCount { get; set; }
        public int ClientSeatsCount { get; set; }
        public int CompanySeatsCount { get; set; }
        public int TotalBusySeats => ClientSeatsCount + CompanySeatsCount;
    }

}
