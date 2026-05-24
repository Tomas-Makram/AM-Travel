using BusinessLayer.DTOs.Company;
using DataLayer.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class SeatBookingBoundPageDTO : IValidatableObject
    {
        public string ActiveTab { get; set; } = "bookTab";

        [Required(ErrorMessage ="Trip date is required")]
        [DataType(DataType.Date)]
        public DateTime? TripDate { get; set; } = DateTime.Today;

        public TripDirection Direction { get; set; } = TripDirection.Departure;

        [MaxLength(200)]
        public string? RouteFrom { get; set; }

        [MaxLength(200)]
        public string? RouteTo { get; set; }

        [Range(1, 100, ErrorMessage = "The number of seats should be between 1 and 100")]
        public int RequiredSeats { get; set; } = 1;

        public Guid? SelectedBusId { get; set; }
        public Guid? SelectedTripId { get; set; }

        public List<SelectListItem> Companies { get; set; } = new();
        public List<string> Locations { get; set; } = new();
        public List<SeatBookingBoundBusDTO> Buses { get; set; } = new();
        public SeatBookingBoundSeatMapDTO? SeatMap { get; set; }

        public CompanySeatBookingSearchDTO Search { get; set; } = new();
        public List<CompanyTripSeatSummaryDTO> SearchResults { get; set; } = new();

        public TransferTripSearchDTO TransferTripSearch { get; set; } = new();
        public List<TransferTripOptionDTO> TransferTrips { get; set; } = new();
        public TransferTripDetailsDTO? TransferTripDetails { get; set; }
        public void Normalize()
        {
            TripDate = TripDate == null || TripDate == default
                ? DateTime.Today
                : TripDate.Value.Date;

            RequiredSeats = Math.Max(1, RequiredSeats);

            RouteFrom = RouteFrom?.Trim();
            RouteTo = RouteTo?.Trim();

            if (SelectedBusId == Guid.Empty)
                SelectedBusId = null;

            if (SelectedTripId == Guid.Empty)
                SelectedTripId = null;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<string>();

            if (RequiredSeats < 1)
                yield return new ValidationResult(
                "The number of seats is required.");

            if (!string.IsNullOrWhiteSpace(RouteFrom) &&
                !string.IsNullOrWhiteSpace(RouteTo) &&
                RouteFrom.Equals(RouteTo, StringComparison.OrdinalIgnoreCase))
                yield return new ValidationResult(
               "The departure location cannot be the same as the return location.");

            if (SelectedBusId.HasValue &&
                (string.IsNullOrWhiteSpace(RouteFrom) || string.IsNullOrWhiteSpace(RouteTo)))
                yield return new ValidationResult(
                "You must select a route before choosing a bus.");

            if (Search.DateFrom.HasValue &&
               Search.DateTo.HasValue &&
               Search.DateFrom.Value.Date > Search.DateTo.Value.Date)
            {
                yield return new ValidationResult(
                    "From Date must be earlier than or equal to To Date.");
            }

            if (!string.IsNullOrWhiteSpace(TransferTripSearch?.RouteFrom) &&
                !string.IsNullOrWhiteSpace(TransferTripSearch?.RouteTo) &&
                TransferTripSearch.RouteFrom.Trim().Equals(
                    TransferTripSearch.RouteTo.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "The departure location cannot be the same as the return location.");
            }
        }
    }
}