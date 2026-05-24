using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Trip
{
    public class ConnectCompanyWithTripsDTO : IValidatableObject
    {
        public Guid CompanyId { get; set; }

        public DateTime TripDate { get; set; }

        public TransportationTripType TripType { get; set; } = TransportationTripType.Departure;

        public Guid? DepartureTripId { get; set; }

        public Guid? ReturnTripId { get; set; }

        public Guid? TripId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CompanyId == Guid.Empty)
                yield return new ValidationResult("Choose the company first.", new[] { nameof(CompanyId) });

            if (TripDate == default)
                yield return new ValidationResult("Choose your flight date.", new[] { nameof(TripDate) });

            if (!Enum.IsDefined(typeof(TransportationTripType), TripType))
                yield return new ValidationResult("Choose trip type.", new[] { nameof(TripType) });

            if (TripType == TransportationTripType.Departure &&
                (!DepartureTripId.HasValue || DepartureTripId.Value == Guid.Empty))
                yield return new ValidationResult("Choose your outbound flight.", new[] { nameof(DepartureTripId) });

            if (TripType == TransportationTripType.Return &&
                (!ReturnTripId.HasValue || ReturnTripId.Value == Guid.Empty))
                yield return new ValidationResult("Choose a return flight.", new[] { nameof(ReturnTripId) });

            if (TripType == TransportationTripType.RoundTrip)
            {
                if (!DepartureTripId.HasValue || DepartureTripId.Value == Guid.Empty)
                    yield return new ValidationResult("Choose your outbound flight.", new[] { nameof(DepartureTripId) });

                if (!ReturnTripId.HasValue || ReturnTripId.Value == Guid.Empty)
                    yield return new ValidationResult("Choose a return flight.", new[] { nameof(ReturnTripId) });

                if (DepartureTripId.HasValue &&
                    ReturnTripId.HasValue &&
                    DepartureTripId.Value != Guid.Empty &&
                    ReturnTripId.Value != Guid.Empty &&
                    DepartureTripId.Value == ReturnTripId.Value)
                    yield return new ValidationResult("Outbound and return flights cannot be the same trip.");
            }
        }

        public IEnumerable<ValidationResult> ValidateForSearch()
        {
            if (CompanyId == Guid.Empty)
                yield return new ValidationResult("Choose the company first.", new[] { nameof(CompanyId) });

            if (TripDate == default)
                yield return new ValidationResult("Choose your flight date.", new[] { nameof(TripDate) });

            if (!Enum.IsDefined(typeof(TransportationTripType), TripType))
                yield return new ValidationResult("Choose trip type.", new[] { nameof(TripType) });
        }

        public IEnumerable<ValidationResult> ValidateForDelete()
        {
            if (CompanyId == Guid.Empty)
                yield return new ValidationResult("Choose the company first.", new[] { nameof(CompanyId) });

            if (TripDate == default)
                yield return new ValidationResult("Choose your flight date.", new[] { nameof(TripDate) });

            if (!TripId.HasValue || TripId.Value == Guid.Empty)
                yield return new ValidationResult("Choose trip to delete.", new[] { nameof(TripId) });
        }
    }
}