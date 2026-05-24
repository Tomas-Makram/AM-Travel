using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class CompanySeatBookingItemDTO
    {
        public Guid BookingId { get; set; }
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;

        public Guid? TripId { get; set; }
        public DateTime? TripDate { get; set; }
        public string TripFromLocation { get; set; } = string.Empty;
        public string TripToLocation { get; set; } = string.Empty;
        public TripDirection? TripDirection { get; set; }
        public Guid? BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public Guid? SeatId { get; set; }
        public int SeatNumber { get; set; }
        public string SeatLabel { get; set; } = string.Empty;
        public SeatType? SeatType { get; set; }

        public int SeatsCount { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public decimal PricePerSeat { get; set; }
        public decimal TotalPrice { get; set; }
        public CompanySeatBookingDirection BookingDirection { get; set; }

        public bool IsTransferredToCompany { get; set; }
        public string? TransferredToCompanyName { get; set; }
        public string? TransferStatusText { get; set; }


        // ── Client Info ───────────────────────────────────────────────
        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }
        public TransportationTripType ClientTripType { get; set; }
        public bool IsRoundTrip { get; set; }

        // ── Return Seat (لو RoundTrip) ────────────────────────────────
        public Guid? ReturnTripId { get; set; }
        public DateTime? ReturnTripDate { get; set; }
        public string ReturnSeatLabel { get; set; } = string.Empty;
        public string ReturnFromLocation { get; set; } = string.Empty;
        public string ReturnToLocation { get; set; } = string.Empty;

        // ── Transfer ──────────────────────────────────────────────────
        public bool IsTransfer { get; set; }
        public Guid? TransferredFromBookingId { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
