using DataLayer.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BusinessLayer.DTOs.Trip
{
    public class GetTripDTO
    {
        public Guid? CompanyId { get; set; }
        public DateTime TripDate { get; set; } = DateTime.Today;
        public TransportationTripType TripType { get; set; } = TransportationTripType.Departure;

        public List<SelectListItem> Companies { get; set; } = new();
        public List<SelectListItem> Locations { get; set; } = new();

        public List<AvailableTripListDTO> LinkedTrips { get; set; } = new();
        public List<AvailableTripListDTO> AccountingTrips { get; set; } = new();

        public TripSearchDTO Search { get; set; } = new();

        public decimal TotalPrice => AccountingTrips.Sum(x => x.CompanyTripPrice);
        public decimal TotalPaid => AccountingTrips.Sum(x => x.CompanyTripPaidAmount);
        public decimal TotalRemaining => AccountingTrips.Sum(x => x.CompanyTripRemainingAmount);
    }
}