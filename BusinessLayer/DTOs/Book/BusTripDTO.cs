using DataLayer.Models;

namespace BusinessLayer.DTOs.Book
{
    public class BusTripDTO
    {
        public Guid TripId { get; set; }
        public Guid BusId { get; set; }

        public string BusName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public TripDirection Direction { get; set; }

        public DateTime TripDate { get; set; }

        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }

        public int SeatsCount { get; set; }
        public int ReservedCount { get; set; }
        public int AvailableCount => SeatsCount - ReservedCount;

        public bool IsClosed { get; set; }
    }
}
