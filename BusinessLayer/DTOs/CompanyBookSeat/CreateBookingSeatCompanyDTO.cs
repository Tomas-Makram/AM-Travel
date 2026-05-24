using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class CreateBookingSeatCompanyDTO : IValidatableObject
    {
        [Required(ErrorMessage = "Company is required.")]
        public Guid CompanyId { get; set; }

        [Required(ErrorMessage = "Bus is required.")]
        public Guid BusId { get; set; }

        public Guid? TripId { get; set; }

        [Required(ErrorMessage = "Trip date is required.")]
        public DateTime TripDate { get; set; }

        [Required(ErrorMessage = "Direction is required.")]
        public TripDirection Direction { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Required seats must be at least 1.")]
        public int RequiredSeats { get; set; } = 1;

        [Required(ErrorMessage = "At least one seat must be selected.")]
        [MinLength(1, ErrorMessage = "At least one seat must be selected.")]
        public List<Guid> SeatIds { get; set; } = new();

        [Required(ErrorMessage = "From location is required.")]
        public string FromLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "To location is required.")]
        public string ToLocation { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Price per seat cannot be negative.")]
        public decimal PricePerSeat { get; set; }

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Client name is required.")]
        public string? ClientName { get; set; }

        [Required(ErrorMessage = "Client phone number is required.")]
        [RegularExpression(
            @"^01[0-2,5][0-9]{8}$",
            ErrorMessage = "Invalid Egyptian phone number format.")]
        public string? ClientPhone { get; set; }

        [Required(ErrorMessage = "Trip type is required.")]
        public TransportationTripType ClientTripType { get; set; } =
            TransportationTripType.Departure;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CompanyId == Guid.Empty)
            {
                yield return new ValidationResult(
                    "Company is required.",
                    new[] { nameof(CompanyId) });
            }

            if (BusId == Guid.Empty)
            {
                yield return new ValidationResult(
                    "Bus is required.",
                    new[] { nameof(BusId) });
            }

            var distinctSeatIds = SeatIds?
                .Distinct()
                .ToList() ?? new List<Guid>();

            if (distinctSeatIds.Count != RequiredSeats)
            {
                yield return new ValidationResult(
                    $"Selected seats must equal required seats ({RequiredSeats}).",
                    new[] { nameof(SeatIds), nameof(RequiredSeats) });
            }

            if (string.Equals(
                    FromLocation?.Trim(),
                    ToLocation?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "From location and To location cannot be the same.",
                    new[] { nameof(FromLocation), nameof(ToLocation) });
            }
        }

        public List<string> ValidateForSeatBookingBoundPage()
        {
            var errors = new List<string>();

            if (TripDate == default)
                errors.Add("Trip date is required.");

            if (!Enum.IsDefined(typeof(TripDirection), Direction))
                errors.Add("Direction is invalid.");

            if (RequiredSeats < 1)
                errors.Add("Required seats must be at least 1.");

            if (BusId == Guid.Empty && TripId.HasValue && TripId.Value != Guid.Empty)
                errors.Add("Bus is required when trip is selected.");

            if (!string.IsNullOrWhiteSpace(FromLocation) &&
                !string.IsNullOrWhiteSpace(ToLocation) &&
                string.Equals(
                    FromLocation.Trim(),
                    ToLocation.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("From location and To location cannot be the same.");
            }

            return errors;
        }
    }
}