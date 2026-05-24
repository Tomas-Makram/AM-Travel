using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Book
{
    public class CreateBookingDTO : IValidatableObject
    {
        [Required(ErrorMessage = "The client name field is required.")]
        [MaxLength(160, ErrorMessage = "Client name cannot be more than 160 characters.")]
        public string ClientName { get; set; } = string.Empty;

        public bool HasHotel { get; set; } = true;
        public bool HasTransportation { get; set; } = false;

        [Display(Name = "hotel name")]
        [MaxLength(180, ErrorMessage = "Hotel name cannot be more than 180 characters.")]
        public string HotelName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

        [Range(0, 100)]
        public int NumberOfRooms { get; set; } = 1;

        public RoomType RoomType { get; set; } = RoomType.Single;

        [Range(0, 100)]
        public int ChildrenCountUntil6Years { get; set; }

        [Range(0, 100)]
        public int ChildrenCountUntil12Years { get; set; }

        [Range(0, 200)]
        public int TotalChildrenCount { get; set; }

        [Range(0, 999999999)]
        public decimal HotelNightPrice { get; set; }

        [Range(1, 365, ErrorMessage = "Nights count must be at least one night.")]
        public int NightsCount { get; set; } = 1;

        [Range(0, 999999999)]
        public decimal HotelTotal { get; set; }

        [Range(0, 1000)]
        public int SeatsCount { get; set; }

        [Range(0, 999999999)]
        public decimal SeatPrice { get; set; }

        [Range(0, 999999999)]
        public decimal TransportationTotal { get; set; }

        public PayType PayType { get; set; } = PayType.cache;

        [Range(0, 999999999)]
        public decimal Discount { get; set; }

        [Range(0, 999999999)]
        public decimal PaidAmount { get; set; }

        [Range(0, 999999999)]
        public decimal GrandTotal { get; set; }

        [Range(0, 999999999)]
        public decimal RemainingAmount { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public List<BookingPaymentDTO> Payments { get; set; } = new();
        public List<BookingRoomDTO> Rooms { get; set; } = new();
        public List<BookingSeatDTO> TransportationSeats { get; set; } = new();
        public TransportationBookingDTO Transportation { get; set; } = new();

        [Required(ErrorMessage = "At least one phone number is required.")]
        [MinLength(1, ErrorMessage = "At least one phone number is required.")]
        public List<BookingPhoneDTO> PhoneNumbers { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!HasHotel && !HasTransportation)
            {
                yield return new ValidationResult(
                    "Please choose hotel booking, transportation booking, or both.",
                    new[] { nameof(HasHotel), nameof(HasTransportation) });
            }

            // ── Hotel validations — فقط لو HasHotel = true ─────────────
            if (HasHotel)
            {
                if (CheckOutDate.Date <= CheckInDate.Date)
                {
                    yield return new ValidationResult(
                        "Check out must be at least one day after check in.",
                        new[] { nameof(CheckOutDate) });
                }

                var nights = (CheckOutDate.Date - CheckInDate.Date).Days;

                if (nights < 1)
                {
                    yield return new ValidationResult(
                        "Minimum booking duration is one night.",
                        new[] { nameof(NightsCount) });
                }

                if (string.IsNullOrWhiteSpace(HotelName))
                {
                    yield return new ValidationResult(
                        "The hotel name field is required.",
                        new[] { nameof(HotelName) });
                }

                if (Rooms == null || Rooms.Count == 0)
                {
                    yield return new ValidationResult(
                        "At least one room row is required.",
                        new[] { nameof(Rooms) });
                }
                else
                {
                    for (var i = 0; i < Rooms.Count; i++)
                    {
                        if (Rooms[i].Count <= 0)
                        {
                            yield return new ValidationResult(
                                $"Room row {i + 1}: count must be greater than zero.",
                                new[] { $"Rooms[{i}].Count" });
                        }

                        if (Rooms[i].NightPrice <= 0)
                        {
                            yield return new ValidationResult(
                                $"Room row {i + 1}: night price must be greater than zero.",
                                new[] { $"Rooms[{i}].NightPrice" });
                        }
                    }
                }
            }

            // ── Transportation validations — فقط لو HasTransportation = true ──
            if (HasTransportation)
            {
                if (Transportation == null)
                {
                    yield return new ValidationResult(
                        "Transportation data is required.",
                        new[] { nameof(Transportation) });

                    yield break;
                }

                if (Transportation.TripType == TransportationTripType.Departure ||
                    Transportation.TripType == TransportationTripType.RoundTrip)
                {
                    if (!Transportation.DepartureDate.HasValue)
                    {
                        yield return new ValidationResult(
                            "Departure date is required.",
                            new[] { "Transportation.DepartureDate" });
                    }

                    var hasExistingDepartureTrip =
                        Transportation.DepartureTripId.HasValue &&
                        Transportation.DepartureTripId.Value != Guid.Empty;

                    var hasNewDepartureBus =
                        Transportation.DepartureBusId.HasValue &&
                        Transportation.DepartureBusId.Value != Guid.Empty;

                    if (!hasExistingDepartureTrip && !hasNewDepartureBus)
                    {
                        yield return new ValidationResult(
                            "Select existing Departure trip or choose Departure bus.",
                            new[] { "Transportation.DepartureTripId", "Transportation.DepartureBusId" });
                    }

                    if (Transportation.DepartureSeats == null || Transportation.DepartureSeats.Count == 0)
                    {
                        yield return new ValidationResult(
                            "Select at least one departure seat.",
                            new[] { "Transportation.DepartureSeats" });
                    }
                }

                if (Transportation.TripType == TransportationTripType.Return ||
                    Transportation.TripType == TransportationTripType.RoundTrip)
                {
                    if (!Transportation.ReturnDate.HasValue)
                    {
                        yield return new ValidationResult(
                            "Return date is required.",
                            new[] { "Transportation.ReturnDate" });
                    }

                    var hasExistingReturnTrip =
                        Transportation.ReturnTripId.HasValue &&
                        Transportation.ReturnTripId.Value != Guid.Empty;

                    var hasNewReturnBus =
                        Transportation.ReturnBusId.HasValue &&
                        Transportation.ReturnBusId.Value != Guid.Empty;

                    if (!hasExistingReturnTrip && !hasNewReturnBus)
                    {
                        yield return new ValidationResult(
                            "Select existing Return trip or choose Return bus.",
                            new[] { "Transportation.ReturnTripId", "Transportation.ReturnBusId" });
                    }

                    if (Transportation.ReturnSeats == null || Transportation.ReturnSeats.Count == 0)
                    {
                        yield return new ValidationResult(
                            "Select at least one return seat.",
                            new[] { "Transportation.ReturnSeats" });
                    }
                }

                if (Transportation.DepartureSeats != null)
                {
                    for (var i = 0; i < Transportation.DepartureSeats.Count; i++)
                    {
                        if (Transportation.DepartureSeats[i].SeatPrice <= 0)
                        {
                            yield return new ValidationResult(
                                $"Go seat {i + 1}: price must be greater than zero.",
                                new[] { $"Transportation.DepartureSeats[{i}].SeatPrice" });
                        }
                    }
                }

                if (Transportation.ReturnSeats != null)
                {
                    for (var i = 0; i < Transportation.ReturnSeats.Count; i++)
                    {
                        if (Transportation.ReturnSeats[i].SeatPrice <= 0)
                        {
                            yield return new ValidationResult(
                                $"Return seat {i + 1}: price must be greater than zero.",
                                new[] { $"Transportation.ReturnSeats[{i}].SeatPrice" });
                        }
                    }
                }
            }

            // ── Phone validations ─────────────────────────────────────────
            if (PhoneNumbers == null || PhoneNumbers.Count == 0)
            {
                yield return new ValidationResult(
                    "At least one phone number is required.",
                    new[] { nameof(PhoneNumbers) });
            }
            else
            {
                if (!PhoneNumbers.Any(x => x.Prime))
                {
                    yield return new ValidationResult(
                        "One primary phone number is required.",
                        new[] { nameof(PhoneNumbers) });
                }

                for (var i = 0; i < PhoneNumbers.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(PhoneNumbers[i].PhoneNumber))
                    {
                        yield return new ValidationResult(
                            $"Phone row {i + 1}: phone number is required.",
                            new[] { $"PhoneNumbers[{i}].PhoneNumber" });
                    }
                }
            }

            // ── Payment validations ───────────────────────────────────────
            if (Payments != null)
            {
                var totalPaid = Payments.Sum(x => x.Amount);

                for (var i = 0; i < Payments.Count; i++)
                {
                    if (Payments[i].Amount < 0)
                    {
                        yield return new ValidationResult(
                            $"Payment row {i + 1}: amount cannot be negative.",
                            new[] { $"Payments[{i}].Amount" });
                    }
                }

                if (totalPaid > GrandTotal)
                {
                    yield return new ValidationResult(
                        "Paid amount cannot be greater than grand total.",
                        new[] { nameof(Payments) });
                }
            }
        }
    }
}