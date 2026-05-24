using DataLayer.Models;

namespace BusinessLayer.DTOs.Trip
{
    public class TripSeatStatusDTO
    {
        public Guid SeatId { get; set; }
        public int SeatNumber { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public SeatType SeatType { get; set; }
        public string? SeatLabel { get; set; }

        public bool IsActive { get; set; }
        public bool IsReserved { get; set; }

        public Guid? BookingId { get; set; }
        public string? ReservedByClient { get; set; }
        public string? BookingCode { get; set; }

        public decimal HotelTotal { get; set; }
        public decimal TransportationTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }

        public DateTime TripDate { get; set; }
        public bool IsCompanyBooking { get; set; }
        public Guid? CompanySeatBookingId { get; set; }
        public Guid? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? PhoneNumbersText { get; set; }
        public string? TripTypeText { get; set; }
        public string? TripRouteText { get; set; }
        public string? SeatRouteFrom { get; set; }
        public string? SeatRouteTo { get; set; }

        public string? CompanyBookingGroupKey { get; set; }
        public string? GeneralRouteFrom { get; set; }
        public string? GeneralRouteTo { get; set; }
        public bool HasHotel { get; set; }
        public bool HasTransportation { get; set; }

        public string? HotelName { get; set; }
        public string? BusName { get; set; }
        public string? PlateNumber { get; set; }
    }
}
