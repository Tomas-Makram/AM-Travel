using BusinessLayer.DTOs.Company;
using DataLayer.Models;

namespace BusinessLayer.DTOs.Trip
{
    public class TripSearchDTO
    {
        public Guid? CompanyId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Location { get; set; }
        public TransportationTripType? TripType { get; set; }
        public CompanyTripPaymentFilter PaymentFilter { get; set; } = CompanyTripPaymentFilter.All;
    }
}
