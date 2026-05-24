using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class TransferredSeatSnapshot
    {
        public Guid BookingSeatId { get; set; }
        public Guid TripId { get; set; }
        public Guid SeatId { get; set; }
        public int Direction { get; set; }

        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public decimal SeatPrice { get; set; }

        public int SeatNumber { get; set; }
        public string SeatLabel { get; set; } = string.Empty;
        public SeatType? SeatType { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }

        public Guid? OriginalCompanyId { get; set; }
        public string? OriginalCompanyName { get; set; }
        public string? OriginalCompanyPhone { get; set; }
        public string? OriginalClientName { get; set; }
        public string? OriginalClientPhone { get; set; }
        public TransportationTripType? OriginalClientTripType { get; set; }
    }
}
