using DataLayer.Models;

namespace BusinessLayer.DTOs.Book
{
    public sealed class BookingDetailsDTO : BookingListItemDTO
    {
        public Guid UserId { get; set; } = Guid.Empty;
        public int ChildrenCountUntil6Years { get; set; }
        public int ChildrenCountUntil12Years { get; set; }
        public int TotalChildrenCount { get; set; }

        public decimal HotelNightPrice { get; set; }
        public int NightsCount { get; set; }

        public int SeatsCount { get; set; }
        public decimal SeatPrice { get; set; }

        public string? Notes { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public List<BookingRoomDTO> Rooms { get; set; } = new();

        public List<BookingPaymentDTO> Payments { get; set; } = new();
        public List<BookingTransportationTripDTO> TransportationTrips { get; set; } = new();
        public List<BookingSeatDTO> TransportationSeats { get; set; } = new();
        public List<BookingPhoneDTO> PhoneNumbers { get; set; } = new();
        public List<BookingAuditDTO> AuditLogs { get; set; } = new();
    }

    public sealed class BookingTransportationTripDTO
    {
        public Guid TripId { get; set; }
        public TripDirection Direction { get; set; }

        public DateTime TripDate { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public Guid BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public int LayoutRows { get; set; }
        public int LayoutColumns { get; set; }

        public decimal Total { get; set; }

        public List<BookingBusSeatDTO> BusSeats { get; set; } = new();
        public List<BookingSeatDTO> BookedSeats { get; set; } = new();
    }

    public sealed class BookingBusSeatDTO
    {
        public Guid SeatId { get; set; }
        public int SeatNumber { get; set; }
        public string SeatLabel { get; set; } = string.Empty;
        public SeatType SeatType { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public bool IsActive { get; set; }
    }
}