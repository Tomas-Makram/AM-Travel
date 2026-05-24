using DataLayer.Models;

namespace BusinessLayer.DTOs.Company
{
    public class CompanySeatBookingDetailsDTO
    {
        public Guid BookingId { get; set; }

        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyPhoneNumber { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;

        public TransportationTripType ClientTripType { get; set; }
        public CompanySeatBookingDirection BookingDirection { get; set; }

        public Guid? TripId { get; set; }
        public DateTime? TripDate { get; set; }
        public string TripRoute { get; set; } = string.Empty;
        public string TripBusName { get; set; } = string.Empty;
        public string TripPlateNumber { get; set; } = string.Empty;
        public string SeatLabel { get; set; } = string.Empty;

        public Guid? ReturnTripId { get; set; }
        public DateTime? ReturnTripDate { get; set; }
        public string ReturnTripRoute { get; set; } = string.Empty;
        public string ReturnBusName { get; set; } = string.Empty;
        public string ReturnPlateNumber { get; set; } = string.Empty;
        public string ReturnSeatLabel { get; set; } = string.Empty;

        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public string ReturnFromLocation { get; set; } = string.Empty;
        public string ReturnToLocation { get; set; } = string.Empty;

        public decimal PricePerSeat { get; set; }
        public decimal TotalPrice { get; set; }

        public bool IsRoundTrip { get; set; }
        public bool IsTransfer { get; set; }
        public Guid? TransferredFromBookingId { get; set; }
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}