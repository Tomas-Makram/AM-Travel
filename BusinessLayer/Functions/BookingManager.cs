using BusinessLayer.DTOs;
using BusinessLayer.DTOs.Book;
using BusinessLayer.DTOs.Book.BusinessLayer.DTOs.Book;
using BusinessLayer.DTOs.Bus;
using BusinessLayer.Models;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace BusinessLayer.Functions
{
    public interface IBookingManager
    {
        Task<BookingFormLookupsDTO> GetFormLookupsAsync();
        Task<ResponceApi<object>> FindByCodeAsync(string code);
        CreateBookingDTO GetCreateDefaults();
        void EnsureFormDefaults(CreateBookingDTO dto);
        Task<ResponceApi<BookingDailyWorkPageDTO>> GetDailyWorkAsync(BookingDailyWorkFilterDTO filter);
        Task<ResponceApi<List<BookingListItemDTO>>> GetAllAsync(string? search = null, DateTime? checkInDate = null, DateTime? checkOutDate = null, string? bookingType = null);
        Task<ResponceApi<BookingDetailsDTO>> GetByIdAsync(Guid bookingId);
        Task<ResponceApi<BookingUserInfoDTO>> GetBookingUserInfoAsync(Guid bookingId);
        Task<ResponceApi<UpdateBookingDTO>> GetUpdateDtoAsync(Guid bookingId);
        Task<ResponceApi<string>> AddPaymentAsync(AddBookingPaymentDTO dto, Guid currentUserId);
        Task<ResponceApi<Guid>> CreateAsync(CreateBookingDTO dto, Guid currentUserId);
        Task<ResponceApi<string>> UpdateAllowedFieldsAsync(UpdateBookingDTO dto, Guid currentUserId, bool isAdmin);
        Task<ResponceApi<string>> SoftDeleteAsync(Guid bookingId, Guid currentUserId);
        Task<ResponceApi<BusLayoutResponseDTO>> GetBusLayoutAsync(Guid? busId, Guid? tripId, DateTime? date, TripDirection? direction);
        Task<ResponceApi<List<Guid>>> GetReservedSeatsAsync(Guid tripId);
        Task<ResponceApi<List<AvailableTripDTO>>> GetAvailableTripsAsync(DateTime date, TripDirection direction);
        Task<ResponceApi<List<string>>> GetRouteToLocationsAsync(string fromLocation, TripDirection? direction = null);
        Task<ResponceApi<List<RouteBusOptionDTO>>> GetRouteBusesAsync(string fromLocation, string toLocation, TripDirection? direction = null);
        Task<ResponceApi<List<RouteBusAvailabilityDTO>>> GetRouteBusesBySeatCount(string fromLocation, string toLocation, TripDirection direction, DateTime tripDate, int requiredSeats);
    }

    public sealed class BookingManager : IBookingManager
    {
        private const string CodeChars = "0123456789";
        private readonly DBContext _db;

        private sealed class TransferredSeatSnapshot
        {
            public Guid BookingSeatId { get; set; }
            public Guid TripId { get; set; }
            public Guid SeatId { get; set; }
            public int Direction { get; set; }

            public string FromLocation { get; set; } = string.Empty;
            public string ToLocation { get; set; } = string.Empty;

            public decimal SeatPrice { get; set; }

            public int SeatNumber { get; set; }
            public string? SeatLabel { get; set; }
            public SeatType? SeatType { get; set; }

            public int RowNumber { get; set; }
            public int ColumnNumber { get; set; }
        }

        public BookingManager(DBContext db)
        {
            _db = db;
        }

        public async Task<ResponceApi<object>> FindByCodeAsync(string code)
        {
            var booking = await _db.Bookings.AsNoTracking()
                .Include(x => x.PhoneNumbers)
                .Include(x => x.TransportationSeats)
                .FirstOrDefaultAsync(x => x.Code == code.Trim() && !x.IsDeleted);

            if (booking == null) return ResponceApi<object>.Fail("Booking not found.");

            return ResponceApi<object>.Ok(new
            {
                bookingId = booking.BookingID,
                code = booking.Code,
                clientName = booking.ClientName,
                phoneNumbers = string.Join(", ", booking.PhoneNumbers.OrderByDescending(p => p.Prime).Select(p => p.PhoneNumber)),
                hasTransportation = booking.HasTransportation,
                seatsCount = booking.TransportationSeats.Count
            });
        }

        public async Task<BookingFormLookupsDTO> GetFormLookupsAsync()
        {
            var buses = await _db.Buses
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.FromLocation)
                .ThenBy(x => x.ToLocation)
                .ThenBy(x => x.Name)
                .Select(x => new BusRouteOptionDTO
                {
                    BusId = x.BusId,
                    BusName = x.Name,
                    PlateNumber = x.PlateNumber ?? string.Empty,
                    FromLocation = x.FromLocation ?? string.Empty,
                    ToLocation = x.ToLocation ?? string.Empty
                })
                .ToListAsync();

            return new BookingFormLookupsDTO
            {
                RoomTypes = Enum.GetValues<RoomType>().Select(x => new LookupOptionDTO { Value = ((int)x).ToString(), Text = x.ToString() }).ToList(),
                PayTypes = Enum.GetValues<PayType>().Select(x => new LookupOptionDTO { Value = ((int)x).ToString(), Text = x.ToString() }).ToList(),
                BusRoutes = buses,
                FromLocations = buses.Where(x => !string.IsNullOrWhiteSpace(x.FromLocation)).Select(x => x.FromLocation.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()
            };
        }

        public CreateBookingDTO GetCreateDefaults()
        {
            return new CreateBookingDTO
            {
                HasHotel = true,
                HasTransportation = false,
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1),
                NightsCount = 1,
                Rooms = new List<BookingRoomDTO> { new() { RoomType = RoomType.Double, Count = 1, NightPrice = 0 } },
                Payments = new List<BookingPaymentDTO> { new() { Amount = 0, PayType = PayType.cache, PaidAt = DateTime.Today } },
                Transportation = new TransportationBookingDTO
                {
                    TripType = TransportationTripType.Departure,
                    DepartureSeats = new List<BookingSeatDTO>(),
                    ReturnSeats = new List<BookingSeatDTO>()
                },
                TransportationSeats = new List<BookingSeatDTO>(),
                PhoneNumbers = new List<BookingPhoneDTO> { new() { PhoneNumber = string.Empty, Prime = true } }
            };
        }

        public void EnsureFormDefaults(CreateBookingDTO dto)
        {
            dto.PhoneNumbers ??= new List<BookingPhoneDTO>();
            dto.Rooms ??= new List<BookingRoomDTO>();
            dto.Payments ??= new List<BookingPaymentDTO>();
            dto.TransportationSeats ??= new List<BookingSeatDTO>();
            dto.Transportation ??= new TransportationBookingDTO();
            dto.Transportation.DepartureSeats ??= new List<BookingSeatDTO>();
            dto.Transportation.ReturnSeats ??= new List<BookingSeatDTO>();

            if (dto.Rooms.Count == 0)
                dto.Rooms.Add(new BookingRoomDTO { RoomType = RoomType.Double, Count = 1, NightPrice = 0 });

            if (dto.Payments.Count == 0)
                dto.Payments.Add(new BookingPaymentDTO { Amount = 0, PayType = PayType.cache, PaidAt = DateTime.Today });

            if (dto.PhoneNumbers.Count == 0)
                dto.PhoneNumbers.Add(new BookingPhoneDTO { PhoneNumber = string.Empty, Prime = true });
        }

        private static List<BookingDailyCompanySeatDTO> BuildCompanySeatDtos(List<CompanySeatBooking> bookings)
        {
            if (!bookings.Any())
                return new List<BookingDailyCompanySeatDTO>();

            return bookings
                .GroupBy(x => new { x.CompanyId, x.FromLocation, x.ToLocation, x.BookingDirection })
                .Select(g =>
                {
                    var seats = g.ToList();

                    return new BookingDailyCompanySeatDTO
                    {
                        CompanyName = seats.First().Company?.Name ?? string.Empty,
                        SeatLabels = string.Join(", ", seats
                            .OrderBy(x => x.SeatNumberSnapshot)
                            .Select(x =>
                                !string.IsNullOrWhiteSpace(x.SeatLabelSnapshot)
                                    ? x.SeatLabelSnapshot
                                    : x.SeatNumberSnapshot > 0
                                        ? x.SeatNumberSnapshot.ToString()
                                        : "?")),
                        SeatsCount = seats.Count,
                        FromLocation = g.Key.FromLocation,
                        ToLocation = g.Key.ToLocation,
                        Total = seats.Sum(x => x.PricePerSeat),
                        BookingDirection = g.Key.BookingDirection
                    };
                })
                .ToList();
        }

        public async Task<ResponceApi<BookingDailyWorkPageDTO>> GetDailyWorkAsync(BookingDailyWorkFilterDTO filter)
        {
            try
            {
                filter ??= new BookingDailyWorkFilterDTO();

                var selectedDate = filter.SelectedDate == default
                    ? DateTime.Today
                    : filter.SelectedDate.Date;

                var workType = string.IsNullOrWhiteSpace(filter.WorkType)
                    ? "transport"
                    : filter.WorkType.Trim().ToLower();

                var direction = filter.Direction ?? TripDirection.Departure;

                var hotelDateType = string.IsNullOrWhiteSpace(filter.HotelDateType)
                    ? "CheckIn"
                    : filter.HotelDateType.Trim();

                var page = new BookingDailyWorkPageDTO
                {
                    SelectedDate = selectedDate,
                    WorkType = workType,
                    Direction = direction,
                    HotelDateType = hotelDateType,
                    WeekDays = BuildRollingWeekDays(selectedDate)
                };

                if (workType == "hotel")
                {
                    var query = _db.Bookings
                        .AsNoTracking()
                        .Include(x => x.PhoneNumbers)
                        .Include(x => x.Rooms)
                        .Where(x => !x.IsDeleted && x.HasHotel);

                    query = hotelDateType.Equals("CheckOut", StringComparison.OrdinalIgnoreCase)
                        ? query.Where(x => x.CheckOutDate.Date == selectedDate)
                        : query.Where(x => x.CheckInDate.Date == selectedDate);

                    var bookings = await query
                        .OrderBy(x => x.HotelName)
                        .ThenBy(x => x.ClientName)
                        .ToListAsync();

                    page.HotelBookings = bookings.Select(x => new BookingDailyHotelItemDTO
                    {
                        BookingId = x.BookingID,
                        Code = x.Code,
                        ClientName = x.ClientName,
                        HotelName = x.HotelName,
                        CheckInDate = x.CheckInDate,
                        CheckOutDate = x.CheckOutDate,
                        NightsCount = x.NightsCount,
                        NumberOfRooms = x.Rooms.Any() ? x.Rooms.Sum(r => r.Count) : x.NumberOfRooms,
                        RoomSummary = x.Rooms.Any()
                            ? string.Join(", ", x.Rooms.OrderBy(r => r.RoomType)
                                .Select(r => $"{r.Count} {r.RoomType} x {r.NightPrice:N2}"))
                            : $"{x.NumberOfRooms} {x.RoomType}",
                        ChildrenCountUntil6Years = x.ChildrenCountUntil6Years,
                        ChildrenCountUntil12Years = x.ChildrenCountUntil12Years,
                        TotalChildrenCount = x.TotalChildrenCount,
                        PhoneNumbersText = string.Join(", ", x.PhoneNumbers
                            .OrderByDescending(p => p.Prime).Select(p => p.PhoneNumber)),
                        HotelTotal = x.HotelTotal,
                        TransportationTotal = x.TransportationTotal,
                        BookingGrandTotal = x.GrandTotal,
                        BookingRemainingAmount = x.RemainingAmount,
                        BookingPaidAmount = x.PaidAmount,
                        HasHotel = x.HasHotel,
                        HasTransportation = x.HasTransportation
                    }).ToList();

                    page.TotalBookingsCount = page.HotelBookings.Count;
                    page.TotalAmount = page.HotelBookings.Sum(x => x.BookingGrandTotal);
                    page.TotalRemaining = page.HotelBookings.Sum(x => x.BookingRemainingAmount);

                    return ResponceApi<BookingDailyWorkPageDTO>.Ok(page, "Daily hotel work loaded successfully.");
                }

                var clientTrips = await _db.BusTrips
                    .AsNoTracking()
                    .Include(x => x.Bus).ThenInclude(x => x.Seats)
                    .Include(x => x.ReservedSeats)
                        .ThenInclude(x => x.Booking)
                            .ThenInclude(x => x.PhoneNumbers)
                    .Include(x => x.ReservedSeats)
                        .ThenInclude(x => x.Booking)
                            .ThenInclude(x => x.Rooms)
                    .Where(x =>
                        !x.IsClosed &&
                        x.TripDate.Date == selectedDate &&
                        x.Direction == direction &&
                        x.ReservedSeats.Any(s => !s.Booking.IsDeleted))
                    .OrderBy(x => x.FromLocation)
                    .ThenBy(x => x.ToLocation)
                    .ThenBy(x => x.BusNameSnapshot)
                    .ToListAsync();

                var companyTripIds = await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Where(x =>
                        x.TripId != null &&
                        x.SeatId != null &&
                        x.Trip!.TripDate.Date == selectedDate &&
                        x.Trip.Direction == direction &&
                        !x.Trip.IsClosed)
                    .Select(x => x.TripId!.Value)
                    .Distinct()
                    .ToListAsync();

                var clientTripIds = clientTrips.Select(x => x.TripId).ToHashSet();
                var companyOnlyTripIds = companyTripIds.Where(id => !clientTripIds.Contains(id)).ToList();

                List<BusTrip> companyOnlyTrips = new();
                if (companyOnlyTripIds.Any())
                {
                    companyOnlyTrips = await _db.BusTrips
                        .AsNoTracking()
                        .Include(x => x.Bus).ThenInclude(x => x.Seats)
                        .Where(x => companyOnlyTripIds.Contains(x.TripId))
                        .ToListAsync();
                }

                var allTripIds = clientTripIds.Union(companyOnlyTripIds).ToList();

                var allCompanySeatBookings = allTripIds.Any()
                    ? await _db.CompanySeatBookings
                        .AsNoTracking()
                        .Include(x => x.Company)
                        .Where(x =>
                            x.TripId != null &&
                            allTripIds.Contains(x.TripId!.Value) &&
                            x.SeatId != null)
                        .ToListAsync()
                    : new List<CompanySeatBooking>();

                var companyBookingsByTrip = allCompanySeatBookings
                    .GroupBy(x => x.TripId!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var tripDtos = clientTrips.Select(trip =>
                {
                    var snapshotSeats = GetSeatsFromSnapshot(trip)
                        .ToDictionary(x => x.SeatId, x => x);

                    var validSeats = trip.ReservedSeats
                        .Where(s => s.Booking != null && !s.Booking.IsDeleted)
                        .ToList();

                    var passengers = validSeats
                        .GroupBy(s => s.BookingId)
                        .Select(g =>
                        {
                            var booking = g.First().Booking;
                            var routeFrom = string.IsNullOrWhiteSpace(trip.FromLocation) ? "-" : trip.FromLocation;
                            var routeTo = string.IsNullOrWhiteSpace(trip.ToLocation) ? "-" : trip.ToLocation;

                            var seatLabels = g.Select(s =>
                            {
                                snapshotSeats.TryGetValue(s.SeatId, out var snap);

                                return new
                                {
                                    Row = snap?.RowNumber ?? 0,
                                    Column = snap?.ColumnNumber ?? 0,
                                    Label = snap == null
                                        ? "?"
                                        : string.IsNullOrWhiteSpace(snap.SeatLabel)
                                            ? snap.SeatNumber.ToString()
                                            : snap.SeatLabel
                                };
                            })
                            .OrderBy(x => x.Row)
                            .ThenBy(x => x.Column)
                            .Select(x => x.Label);

                            return new BookingDailyTransportPassengerDTO
                            {
                                BookingId = booking.BookingID,
                                Code = booking.Code,
                                ClientName = booking.ClientName,
                                HotelName = booking.HotelName,
                                PhoneNumbersText = string.Join(", ", booking.PhoneNumbers
                                    .OrderByDescending(p => p.Prime).Select(p => p.PhoneNumber)),
                                RouteText = $"{routeFrom} → {routeTo}",
                                SeatLabelsText = string.Join(", ", seatLabels),
                                SeatsCount = g.Count(),
                                TransportationTotal = g.Sum(s => s.SeatPrice),
                                BookingGrandTotal = booking.GrandTotal,
                                BookingRemainingAmount = booking.RemainingAmount,
                                BookingPaidAmount = booking.PaidAmount,
                                HasHotel = booking.HasHotel,
                                HasTransportation = booking.HasTransportation
                            };
                        })
                        .OrderBy(x => x.ClientName)
                        .ToList();

                    var allSeatsForTrip = companyBookingsByTrip.TryGetValue(trip.TripId, out var cs) ? cs : new();
                    var companyClients = BuildCompanyClientDtos(allSeatsForTrip.Where(x => !string.IsNullOrWhiteSpace(x.ClientName)).ToList(), _db);
                    var companySeats = BuildCompanySeatDtos(allSeatsForTrip.Where(x => string.IsNullOrWhiteSpace(x.ClientName)).ToList());

                    return new BookingDailyTransportTripDTO
                    {
                        TripId = trip.TripId,
                        Direction = trip.Direction,
                        TripDate = trip.TripDate,
                        BusId = trip.BusId,
                        BusName = !string.IsNullOrWhiteSpace(trip.BusNameSnapshot) ? trip.BusNameSnapshot : trip.Bus.Name,
                        PlateNumber = !string.IsNullOrWhiteSpace(trip.PlateNumberSnapshot) ? trip.PlateNumberSnapshot : trip.Bus.PlateNumber ?? string.Empty,
                        FromLocation = trip.FromLocation ?? string.Empty,
                        ToLocation = trip.ToLocation ?? string.Empty,
                        BookingsCount = passengers.Count,
                        SeatsCount = validSeats.Count,
                        TripTotal = validSeats.Sum(x => x.SeatPrice),
                        TripRemaining = passengers.Sum(x => x.BookingRemainingAmount),
                        Passengers = passengers,
                        CompanySeatsCount = companySeats.Sum(x => x.SeatsCount),
                        CompanySeatsTotal = companySeats.Sum(x => x.Total),
                        CompanySeats = companySeats,
                        CompanyClients = companyClients
                    };
                }).ToList();

                foreach (var trip in companyOnlyTrips)
                {
                    var allSeatsForTrip = companyBookingsByTrip.TryGetValue(trip.TripId, out var cs) ? cs : new();
                    var companyClients = BuildCompanyClientDtos(allSeatsForTrip.Where(x => !string.IsNullOrWhiteSpace(x.ClientName)).ToList(), _db);
                    var companySeats = BuildCompanySeatDtos(allSeatsForTrip.Where(x => string.IsNullOrWhiteSpace(x.ClientName)).ToList());

                    tripDtos.Add(new BookingDailyTransportTripDTO
                    {
                        TripId = trip.TripId,
                        Direction = trip.Direction,
                        TripDate = trip.TripDate,
                        BusId = trip.BusId,
                        BusName = !string.IsNullOrWhiteSpace(trip.BusNameSnapshot) ? trip.BusNameSnapshot : trip.Bus.Name,
                        PlateNumber = !string.IsNullOrWhiteSpace(trip.PlateNumberSnapshot) ? trip.PlateNumberSnapshot : trip.Bus.PlateNumber ?? string.Empty,
                        FromLocation = trip.FromLocation ?? string.Empty,
                        ToLocation = trip.ToLocation ?? string.Empty,
                        BookingsCount = 0,
                        SeatsCount = 0,
                        TripTotal = 0,
                        TripRemaining = 0,
                        Passengers = new List<BookingDailyTransportPassengerDTO>(),
                        CompanySeatsCount = companySeats.Sum(x => x.SeatsCount),
                        CompanySeatsTotal = companySeats.Sum(x => x.Total),
                        CompanySeats = companySeats,
                        CompanyClients = companyClients
                    });
                }

                page.TransportTrips = tripDtos
                    .OrderBy(x => x.FromLocation)
                    .ThenBy(x => x.ToLocation)
                    .ThenBy(x => x.BusName)
                    .ToList();

                page.TotalTripsCount = page.TransportTrips.Count;
                page.TotalBookingsCount = page.TransportTrips.Sum(x => x.BookingsCount + x.CompanyClients.Count);
                page.TotalSeatsCount = page.TransportTrips.Sum(x =>
                    x.SeatsCount + x.CompanySeatsCount + x.CompanyClients.Sum(c => c.HasReturnSeat ? 2 : 1));
                page.TotalAmount = page.TransportTrips.Sum(x =>
                    x.TripTotal + x.CompanySeatsTotal + x.CompanyClients.Sum(c => c.TotalPrice));
                page.TotalRemaining = page.TransportTrips.Sum(x => x.TripRemaining);

                return ResponceApi<BookingDailyWorkPageDTO>.Ok(page, "Daily transport work loaded successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<BookingDailyWorkPageDTO>.Fail("Failed to load daily work.", ex.Message);
            }
        }

        private static List<BookingDailyCompanyClientDTO> BuildCompanyClientDtos(List<CompanySeatBooking> bookings, DBContext db)
        {
            if (!bookings.Any())
                return new List<BookingDailyCompanyClientDTO>();

            var transferredBookingIds = bookings
                .Where(x => x.IsTransfer && x.TransferredFromBookingId.HasValue)
                .Select(x => x.TransferredFromBookingId!.Value)
                .Distinct()
                .ToList();

            var bookingCodes = new Dictionary<Guid, string>();

            if (transferredBookingIds.Any())
            {
                bookingCodes = db.Bookings
                    .AsNoTracking()
                    .Where(x => transferredBookingIds.Contains(x.BookingID))
                    .Select(x => new { x.BookingID, x.Code })
                    .ToDictionary(x => x.BookingID, x => x.Code);
            }

            return bookings
                .GroupBy(x => new
                {
                    x.CompanyId,
                    x.ClientName,
                    x.ClientPhone,
                    x.ClientTripType,
                    x.FromLocation,
                    x.ToLocation
                })
                .Select(g =>
                {
                    var seats = g.ToList();
                    var first = seats.First();

                    string? returnSeatLabel = null;
                    Guid? returnTripId = null;
                    bool hasReturnSeat = false;

                    if (first.ClientTripType == TransportationTripType.RoundTrip && first.ReturnTripId.HasValue)
                    {
                        hasReturnSeat = true;
                        returnTripId = first.ReturnTripId;
                        returnSeatLabel =
                            !string.IsNullOrWhiteSpace(first.ReturnSeatLabelSnapshot)
                                ? first.ReturnSeatLabelSnapshot
                                : first.ReturnSeatNumberSnapshot > 0
                                    ? first.ReturnSeatNumberSnapshot.ToString()
                                    : "?";
                    }

                    string? originalCode = null;
                    if (first.IsTransfer && first.TransferredFromBookingId.HasValue)
                        bookingCodes.TryGetValue(first.TransferredFromBookingId.Value, out originalCode);

                    return new BookingDailyCompanyClientDTO
                    {
                        BookingId = first.BookingId,
                        CompanyName = first.Company?.Name ?? string.Empty,
                        ClientName = g.Key.ClientName,
                        ClientPhone = g.Key.ClientPhone,
                        SeatLabel = string.Join(", ", seats
                            .OrderBy(x => x.SeatNumberSnapshot)
                            .Select(x =>
                                !string.IsNullOrWhiteSpace(x.SeatLabelSnapshot)
                                    ? x.SeatLabelSnapshot
                                    : x.SeatNumberSnapshot > 0
                                        ? x.SeatNumberSnapshot.ToString()
                                        : "?")),
                        HasReturnSeat = hasReturnSeat,
                        ReturnSeatLabel = returnSeatLabel,
                        ReturnTripId = returnTripId,
                        FromLocation = g.Key.FromLocation,
                        ToLocation = g.Key.ToLocation,
                        ClientTripType = g.Key.ClientTripType,
                        PricePerSeat = first.PricePerSeat,
                        TotalPrice = seats.Sum(x => x.PricePerSeat),
                        IsTransfer = first.IsTransfer,
                        OriginalBookingCode = originalCode
                    };
                })
                .ToList();
        }

        private static List<BookingDailyWorkDayDTO> BuildRollingWeekDays(DateTime selectedDate)
        {
            var today = DateTime.Today;
            var days = new List<BookingDailyWorkDayDTO>();

            for (var i = 0; i < 7; i++)
            {
                var date = today.AddDays(i).Date;

                days.Add(new BookingDailyWorkDayDTO
                {
                    Date = date,
                    DayName = date.ToString("dddd", CultureInfo.InvariantCulture),
                    ShortDayName = date.ToString("ddd", CultureInfo.InvariantCulture),
                    IsSelected = date == selectedDate.Date,
                    IsToday = date == today
                });
            }

            if (!days.Any(x => x.Date == selectedDate.Date))
            {
                days.Insert(0, new BookingDailyWorkDayDTO
                {
                    Date = selectedDate.Date,
                    DayName = selectedDate.ToString("dddd", CultureInfo.InvariantCulture),
                    ShortDayName = selectedDate.ToString("ddd", CultureInfo.InvariantCulture),
                    IsSelected = true,
                    IsToday = selectedDate.Date == today
                });
            }

            return days;
        }

        public async Task<ResponceApi<List<BookingListItemDTO>>> GetAllAsync(string? search = null, DateTime? checkInDate = null, DateTime? checkOutDate = null, string? bookingType = null)
        {
            try
            {
                var today = DateTime.Today;
                var query = _db.Bookings.AsNoTracking().Where(b => !b.IsDeleted);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.Trim().ToLower();
                    query = query.Where(b => b.Code.ToLower().Contains(s) || b.ClientName.ToLower().Contains(s) || b.HotelName.ToLower().Contains(s) || b.User.FullName.ToLower().Contains(s) || b.User.UserName.ToLower().Contains(s) || b.PhoneNumbers.Any(p => p.PhoneNumber.ToLower().Contains(s)));
                }

                if (checkInDate.HasValue)
                    query = query.Where(b => b.CheckInDate.Date == checkInDate.Value.Date);

                if (checkOutDate.HasValue)
                    query = query.Where(b => b.CheckOutDate.Date == checkOutDate.Value.Date);

                query = bookingType switch
                {
                    "hotel" => query.Where(b => b.HasHotel && !b.HasTransportation),
                    "transport" => query.Where(b => !b.HasHotel && b.HasTransportation),
                    "both" => query.Where(b => b.HasHotel && b.HasTransportation),
                    _ => query
                };

                var bookings = await query
                    .Include(b => b.User)
                    .Include(b => b.PhoneNumbers)
                    .Include(b => b.Rooms)
                    .Include(b => b.Payments)
                    .Include(b => b.TransportationSeats)
                    .OrderByDescending(b => b.CreatedAtUtc)
                    .ToListAsync();

                var data = bookings.Select(ToListItem)
                    .OrderBy(x => GetRowPriority(x, today))
                    .ThenByDescending(x => x.CheckInDate)
                    .ThenByDescending(x => x.CreatedAtUtc)
                    .ToList();

                return ResponceApi<List<BookingListItemDTO>>.Ok(data, "Bookings loaded successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<BookingListItemDTO>>.Fail("Failed to load bookings.", ex.Message);
            }
        }

        public async Task<ResponceApi<BookingDetailsDTO>> GetByIdAsync(Guid bookingId)
        {
            try
            {
                var booking = await BuildDetailsQuery().FirstOrDefaultAsync(b => b.BookingID == bookingId && !b.IsDeleted);

                if (booking == null)
                    return ResponceApi<BookingDetailsDTO>.Fail("Booking not found.");

                return ResponceApi<BookingDetailsDTO>.Ok(ToDetails(booking), "Booking loaded successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<BookingDetailsDTO>.Fail("Failed to load booking.", ex.Message);
            }
        }

        public async Task<ResponceApi<BookingUserInfoDTO>> GetBookingUserInfoAsync(Guid bookingId)
        {
            try
            {
                if (bookingId == Guid.Empty)
                    return ResponceApi<BookingUserInfoDTO>.Fail("Booking id is required.");

                var user = await _db.Bookings
                    .AsNoTracking()
                    .Where(x => x.BookingID == bookingId && !x.IsDeleted)
                    .Select(x => new BookingUserInfoDTO
                    {
                        BookingId = x.BookingID,
                        UserId = x.User.UserId,
                        FullName = x.User.FullName,
                        UserName = x.User.UserName,
                        PhoneNumber = x.User.PhoneNumber
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return ResponceApi<BookingUserInfoDTO>.Fail("Booking user not found.");

                return ResponceApi<BookingUserInfoDTO>.Ok(user, "Booking user loaded successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<BookingUserInfoDTO>.Fail("Failed to load booking user.", ex.Message);
            }
        }

        public async Task<ResponceApi<UpdateBookingDTO>> GetUpdateDtoAsync(Guid bookingId)
        {
            var result = await GetByIdAsync(bookingId);
            if (!result.Success || result.Data == null)
                return ResponceApi<UpdateBookingDTO>.Fail(result.Message ?? "Booking not found.", result.Errors?.ToArray() ?? Array.Empty<string>());

            var b = result.Data;
            var goTrip = b.TransportationTrips.FirstOrDefault(x => x.Direction == TripDirection.Departure);
            var returnTrip = b.TransportationTrips.FirstOrDefault(x => x.Direction == TripDirection.Return);

            var dto = new UpdateBookingDTO
            {
                BookingID = b.BookingID,
                Code = b.Code,
                ClientName = b.ClientName,
                HasHotel = b.HasHotel,
                HasTransportation = b.HasTransportation,
                HotelName = b.HotelName,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                NumberOfRooms = b.NumberOfRooms <= 0 ? 1 : b.NumberOfRooms,
                RoomType = Enum.TryParse<RoomType>(b.RoomTypeName, out var roomType) ? roomType : RoomType.Single,
                ChildrenCountUntil6Years = b.ChildrenCountUntil6Years,
                ChildrenCountUntil12Years = b.ChildrenCountUntil12Years,
                TotalChildrenCount = b.TotalChildrenCount,
                HotelNightPrice = b.HotelNightPrice,
                NightsCount = b.NightsCount <= 0 ? 1 : b.NightsCount,
                HotelTotal = b.HotelTotal,
                SeatsCount = b.SeatsCount,
                SeatPrice = b.SeatPrice,
                TransportationTotal = b.TransportationTotal,
                PayType = Enum.TryParse<PayType>(b.PayTypeName, out var payType) ? payType : PayType.cache,
                Discount = b.Discount,
                PaidAmount = b.PaidAmount,
                GrandTotal = b.GrandTotal,
                RemainingAmount = b.RemainingAmount,
                Notes = b.Notes,
                PhoneNumbers = b.PhoneNumbers.Count > 0 ? b.PhoneNumbers : new List<BookingPhoneDTO> { new() { Prime = true } },
                Rooms = b.Rooms.Count > 0 ? b.Rooms : new List<BookingRoomDTO> { new() { RoomType = RoomType.Single, Count = 1, NightPrice = b.HotelNightPrice } },
                Payments = b.Payments.Count > 0 ? b.Payments : new List<BookingPaymentDTO> { new() { Amount = 0, PayType = PayType.cache, PaidAt = DateTime.Today } },
                TransportationSeats = b.TransportationSeats,
                Transportation = new TransportationBookingDTO
                {
                    TripType = goTrip != null && returnTrip != null ? TransportationTripType.RoundTrip : returnTrip != null ? TransportationTripType.Return : TransportationTripType.Departure,
                    DepartureTripId = goTrip?.TripId,
                    ReturnTripId = returnTrip?.TripId,
                    DepartureBusId = goTrip?.BusId,
                    ReturnBusId = returnTrip?.BusId,
                    DepartureDate = goTrip?.TripDate,
                    ReturnDate = returnTrip?.TripDate,
                    DepartureFromLocation = goTrip?.FromLocation ?? string.Empty,
                    DepartureToLocation = goTrip?.ToLocation ?? string.Empty,
                    ReturnFromLocation = returnTrip?.FromLocation ?? string.Empty,
                    ReturnToLocation = returnTrip?.ToLocation ?? string.Empty,
                    DepartureSeats = b.TransportationSeats.Where(x => x.Direction == TripDirection.Departure).ToList(),
                    ReturnSeats = b.TransportationSeats.Where(x => x.Direction == TripDirection.Return).ToList(),
                    CreateNewDepartureTrip = false,
                    CreateNewReturnTrip = false
                }
            };

            EnsureFormDefaults(dto);
            return ResponceApi<UpdateBookingDTO>.Ok(dto, "Booking loaded successfully.");
        }

        //---------------------------------------------------------------------------------------------//

        private BookingData CreateBookingEntity(CreateBookingDTO dto, Guid currentUserId)
        {
            return new BookingData
            {
                BookingID = Guid.NewGuid(),
                Code = GenerateUniqueCodeAsync().GetAwaiter().GetResult(),
                UserId = currentUserId,
                ClientName = dto.ClientName.Trim(),
                HasHotel = dto.HasHotel,
                HasTransportation = dto.HasTransportation,
                HotelName = dto.HasHotel ? dto.HotelName.Trim() : string.Empty,
                CheckInDate = dto.CheckInDate.Date,
                CheckOutDate = dto.CheckOutDate.Date,
                NumberOfRooms = dto.HasHotel ? dto.Rooms.Sum(x => x.Count) : 0,
                RoomType = dto.Rooms.FirstOrDefault()?.RoomType ?? dto.RoomType,
                ChildrenCountUntil6Years = dto.HasHotel ? dto.ChildrenCountUntil6Years : 0,
                ChildrenCountUntil12Years = dto.HasHotel ? dto.ChildrenCountUntil12Years : 0,
                TotalChildrenCount = dto.HasHotel ? dto.ChildrenCountUntil6Years + dto.ChildrenCountUntil12Years : 0,
                HotelNightPrice = dto.HasHotel ? dto.Rooms.FirstOrDefault()?.NightPrice ?? 0 : 0,
                NightsCount = dto.HasHotel ? dto.NightsCount : 0,
                HotelTotal = dto.HasHotel ? dto.HotelTotal : 0,
                SeatsCount = dto.HasTransportation ? dto.TransportationSeats.Count : 0,
                SeatPrice = dto.HasTransportation ? dto.TransportationSeats.FirstOrDefault()?.SeatPrice ?? 0 : 0,
                TransportationTotal = dto.HasTransportation ? dto.TransportationTotal : 0,
                PayType = dto.Payments.FirstOrDefault()?.PayType ?? dto.PayType,
                Discount = dto.Discount,
                PaidAmount = dto.PaidAmount,
                GrandTotal = dto.GrandTotal,
                RemainingAmount = dto.RemainingAmount,
                Notes = dto.Notes,
                IsDeleted = false,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = null
            };
        }

        private async Task<ResponceApi<string>> ValidateCreateOrUpdate(CreateBookingDTO dto, Guid? bookingId = null)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.ClientName)) errors.Add("Client name is required.");
            if (!dto.HasHotel && !dto.HasTransportation) errors.Add("Choose hotel, transportation, or both.");

            if (dto.HasHotel)
            {
                if (string.IsNullOrWhiteSpace(dto.HotelName)) errors.Add("Hotel name is required.");
                if (dto.CheckOutDate.Date <= dto.CheckInDate.Date) errors.Add("Check-out date must be after check-in date.");
                if (dto.Rooms == null || !dto.Rooms.Any(x => x.Count > 0)) errors.Add("At least one room type is required.");
                foreach (var room in dto.Rooms ?? new List<BookingRoomDTO>())
                {
                    if (room.Count < 1) errors.Add("Room count must be at least 1.");
                    if (room.NightPrice < 0) errors.Add("Room night price cannot be negative.");
                }
            }

            ValidateTransportation(dto, errors);

            if (dto.ChildrenCountUntil6Years < 0 || dto.ChildrenCountUntil12Years < 0 || dto.TotalChildrenCount < 0)
                errors.Add("Children counts cannot be negative.");

            if (dto.TotalChildrenCount != dto.ChildrenCountUntil6Years + dto.ChildrenCountUntil12Years)
                errors.Add("Total children count must equal children under 6 plus children under 12.");

            if (dto.Discount < 0) errors.Add("Discount cannot be negative.");
            foreach (var payment in dto.Payments ?? new List<BookingPaymentDTO>())
                if (payment.Amount < 0) errors.Add("Payment amount cannot be negative.");

            await Task.CompletedTask;
            return errors.Count == 0 ? ResponceApi<string>.Ok("Valid") : ResponceApi<string>.Fail("Booking data is invalid.", errors.ToArray());
        }

        private static void ValidateTransportation(CreateBookingDTO dto, List<string> errors)
        {
            if (!dto.HasTransportation)
                return;

            if (dto.Transportation == null)
            {
                errors.Add("Transportation data is required.");
                return;
            }

            ValidateTripSide(
                tripType: dto.Transportation.TripType,
                sideType: TransportationTripType.Departure,
                date: dto.Transportation.DepartureDate,
                busId: dto.Transportation.DepartureBusId,
                tripId: dto.Transportation.DepartureTripId,
                createNewTrip: dto.Transportation.CreateNewDepartureTrip,
                from: dto.Transportation.DepartureFromLocation,
                to: dto.Transportation.DepartureToLocation,
                requiredSeats: dto.Transportation.DepartureRequiredSeats,
                seats: dto.Transportation.DepartureSeats,
                label: "Go",
                errors: errors);

            ValidateTripSide(
                tripType: dto.Transportation.TripType,
                sideType: TransportationTripType.Return,
                date: dto.Transportation.ReturnDate,
                busId: dto.Transportation.ReturnBusId,
                tripId: dto.Transportation.ReturnTripId,
                createNewTrip: dto.Transportation.CreateNewReturnTrip,
                from: dto.Transportation.ReturnFromLocation,
                to: dto.Transportation.ReturnToLocation,
                requiredSeats: dto.Transportation.ReturnRequiredSeats,
                seats: dto.Transportation.ReturnSeats,
                label: "Return",
                errors: errors);
        }

        private static void ValidateTripSide(TransportationTripType tripType, TransportationTripType sideType, DateTime? date, Guid? busId, Guid? tripId, bool createNewTrip, string? from, string? to, int requiredSeats, List<BookingSeatDTO>? seats, string label, List<string> errors)
        {
            if (tripType != sideType && tripType != TransportationTripType.RoundTrip)
                return;

            seats ??= new List<BookingSeatDTO>();

            if (requiredSeats < 1)
                errors.Add($"{label} required seats count is required.");

            if (!date.HasValue)
                errors.Add($"{label} date is required.");

            if (createNewTrip)
            {
                if (!busId.HasValue || busId.Value == Guid.Empty)
                    errors.Add($"{label} bus is required.");
            }
            else
            {
                if (!tripId.HasValue || tripId.Value == Guid.Empty)
                    errors.Add($"Select existing {label} trip or choose create new trip.");
            }

            if (string.IsNullOrWhiteSpace(from))
                errors.Add($"{label} from location is required.");

            if (string.IsNullOrWhiteSpace(to))
                errors.Add($"{label} to location is required.");

            if (seats.Count == 0)
                errors.Add($"Select at least one {label} seat.");

            if (requiredSeats > 0 && seats.Count != requiredSeats)
                errors.Add($"{label} selected seats must equal required seats count ({requiredSeats}).");

            if (seats.GroupBy(x => x.SeatId).Any(g => g.Count() > 1))
                errors.Add($"Duplicate {label} seat selection is not allowed.");

            foreach (var seat in seats)
            {
                if (seat.SeatId == Guid.Empty)
                    errors.Add($"Invalid {label} seat.");

                if (seat.SeatPrice <= 0)
                    errors.Add($"{label} seat price must be greater than zero.");

                if (string.IsNullOrWhiteSpace(seat.FromLocation))
                    errors.Add($"{label} seat from location is required.");

                if (string.IsNullOrWhiteSpace(seat.ToLocation))
                    errors.Add($"{label} seat to location is required.");
            }
        }

        private async Task AddTransportationAsync(BookingData booking, CreateBookingDTO dto, Guid? ignoreBookingId = null)
        {
            if (!dto.HasTransportation || dto.Transportation == null) return;

            if (dto.Transportation.TripType == TransportationTripType.Departure || dto.Transportation.TripType == TransportationTripType.RoundTrip)
            {
                var trip = await GetOrCreateTripAsync(dto.Transportation.DepartureTripId, dto.Transportation.DepartureBusId, TripDirection.Departure, dto.Transportation.DepartureDate!.Value, dto.Transportation.CreateNewDepartureTrip, dto.Transportation.DepartureFromLocation, dto.Transportation.DepartureToLocation);
                await AddTransportationSeatsAsync(booking, trip.TripId, dto.Transportation.DepartureSeats, TripDirection.Departure, ignoreBookingId);
            }

            if (dto.Transportation.TripType == TransportationTripType.Return || dto.Transportation.TripType == TransportationTripType.RoundTrip)
            {
                var trip = await GetOrCreateTripAsync(dto.Transportation.ReturnTripId, dto.Transportation.ReturnBusId, TripDirection.Return, dto.Transportation.ReturnDate!.Value, dto.Transportation.CreateNewReturnTrip, dto.Transportation.ReturnFromLocation, dto.Transportation.ReturnToLocation);
                await AddTransportationSeatsAsync(booking, trip.TripId, dto.Transportation.ReturnSeats, TripDirection.Return, ignoreBookingId);
            }
        }

        private async Task AddTransportationSeatsAsync(BookingData booking, Guid tripId, IEnumerable<BookingSeatDTO> seats, TripDirection direction, Guid? ignoreBookingId)
        {
            var selected = seats.Where(x => x.SeatId != Guid.Empty).ToList();

            var trip = await _db.BusTrips
                .Include(x => x.Bus)
                    .ThenInclude(x => x.Seats)
                .FirstOrDefaultAsync(x => x.TripId == tripId && !x.IsClosed);

            if (trip == null)
                throw new InvalidOperationException("Trip not found or closed.");

            var snapshotSeats = GetSeatsFromSnapshot(trip);
            var snapshotSeatIds = snapshotSeats
                .Where(x => x.IsActive)
                .Select(x => x.SeatId)
                .ToHashSet();

            if (selected.Any(x => !snapshotSeatIds.Contains(x.SeatId)))
                throw new InvalidOperationException("One or more selected seats are not valid in this trip snapshot.");

            var available = await AreSeatsAvailableAsync(
                tripId,
                selected.Select(x => x.SeatId),
                ignoreBookingId);

            if (!available)
                throw new InvalidOperationException(
                    direction == TripDirection.Return
                        ? "One or more Return seats are already reserved."
                        : "One or more Go seats are already reserved.");

            foreach (var seat in selected)
            {
                booking.TransportationSeats.Add(new BookingTransportationSeat
                {
                    BookingSeatId = Guid.NewGuid(),
                    BookingId = booking.BookingID,
                    TripId = tripId,
                    SeatId = seat.SeatId,
                    Direction = direction,
                    SeatPrice = seat.SeatPrice,
                    FromLocation = seat.FromLocation,
                    ToLocation = seat.ToLocation,
                    ReservedAtUtc = DateTime.UtcNow
                });
            }
        }

        private static void AddCreateAuditDetails(BookingAudit audit, BookingData booking)
        {
            AddChange(audit, "Code", null, booking.Code);
            AddChange(audit, nameof(booking.ClientName), null, booking.ClientName);
            AddChange(audit, nameof(booking.HasHotel), null, booking.HasHotel);
            AddChange(audit, nameof(booking.HasTransportation), null, booking.HasTransportation);
            AddChange(audit, nameof(booking.HotelTotal), null, booking.HotelTotal);
            AddChange(audit, nameof(booking.TransportationTotal), null, booking.TransportationTotal);
            AddChange(audit, nameof(booking.GrandTotal), null, booking.GrandTotal);
        }

        public async Task<ResponceApi<Guid>> CreateAsync(CreateBookingDTO dto, Guid currentUserId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                CalculateTotals(dto);
                var validation = await ValidateCreateOrUpdate(dto);
                if (!validation.Success)
                    return ResponceApi<Guid>.Fail(validation.Message ?? "Invalid booking data.", validation.Errors?.ToArray() ?? Array.Empty<string>());

                if (!await _db.Users.AnyAsync(u => u.UserId == currentUserId))
                    return ResponceApi<Guid>.Fail("Invalid current user.");

                var booking = CreateBookingEntity(dto, currentUserId);

                AddPhones(booking, dto.PhoneNumbers);
                AddRooms(booking, dto);
                AddPayments(booking, dto.Payments);
                await AddTransportationAsync(booking, dto);

                var audit = CreateAudit(booking.BookingID, currentUserId, "Create", "Booking created.");
                AddCreateAuditDetails(audit, booking);
                booking.AuditLogs.Add(audit);

                _db.Bookings.Add(booking);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<Guid>.Ok(booking.BookingID, $"Booking created successfully. Code: {booking.Code}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<Guid>.Fail("Failed to create booking.", ex.Message);
            }
        }

        //------------------------------------------------------------------------------------------//

        public async Task<ResponceApi<string>> UpdateAllowedFieldsAsync(UpdateBookingDTO dto, Guid currentUserId, bool isAdmin)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (dto.BookingID == Guid.Empty)
                    return ResponceApi<string>.Fail("Booking id is required.");

                var booking = await _db.Bookings
                    .Include(x => x.Payments)
                    .Include(x => x.TransportationSeats)
                        .ThenInclude(x => x.Trip)
                    .FirstOrDefaultAsync(x => x.BookingID == dto.BookingID && !x.IsDeleted);

                if (booking == null)
                    return ResponceApi<string>.Fail("Booking not found.");

                var oldPhonesList = await _db.Telephones
                    .AsNoTracking()
                    .Where(x => x.BookingID == booking.BookingID)
                    .ToListAsync();

                var oldRoomsList = await _db.BookingRooms
                    .AsNoTracking()
                    .Where(x => x.BookingId == booking.BookingID)
                    .ToListAsync();

                var errors = ValidateAllowedEditDto(dto, booking, isAdmin);
                if (errors.Count > 0)
                    return ResponceApi<string>.Fail("Booking data is invalid.", errors.ToArray());

                var audit = CreateAudit(
                    booking.BookingID,
                    currentUserId,
                    "LimitedUpdate",
                    "Allowed booking fields updated.");

                var oldHotelTotal = booking.HotelTotal;
                var oldGrandTotal = booking.GrandTotal;
                var oldRemaining = booking.RemainingAmount;
                var oldTripType = GetCurrentTripType(booking).ToString();
                var oldPhones = FormatPhones(oldPhonesList);
                var oldRooms = FormatRooms(oldRoomsList);
                var oldChildren6 = booking.ChildrenCountUntil6Years;
                var oldChildren12 = booking.ChildrenCountUntil12Years;
                var oldChildrenTotal = booking.TotalChildrenCount;

                if (booking.HasHotel)
                {
                    var newCheckIn = dto.CheckInDate.Date;
                    var newCheckOut = dto.CheckOutDate.Date;
                    var newNights = Math.Max(1, (newCheckOut - newCheckIn).Days);
                    var newHotelName = dto.HotelName?.Trim() ?? string.Empty;
                    var newRooms = NormalizeRooms(dto.Rooms).ToList();

                    AddChange(audit, nameof(booking.HotelName), booking.HotelName, newHotelName);
                    AddChange(audit, nameof(booking.CheckInDate), booking.CheckInDate, newCheckIn);
                    AddChange(audit, nameof(booking.CheckOutDate), booking.CheckOutDate, newCheckOut);
                    AddChange(audit, nameof(booking.NightsCount), booking.NightsCount, newNights);
                    AddChange(audit, "Rooms", oldRooms, FormatRooms(newRooms));
                    AddChange(audit, nameof(booking.ChildrenCountUntil6Years), oldChildren6, dto.ChildrenCountUntil6Years);
                    AddChange(audit, nameof(booking.ChildrenCountUntil12Years), oldChildren12, dto.ChildrenCountUntil12Years);
                    AddChange(audit, nameof(booking.TotalChildrenCount), oldChildrenTotal, dto.ChildrenCountUntil6Years + dto.ChildrenCountUntil12Years);

                    booking.HotelName = newHotelName;
                    booking.CheckInDate = newCheckIn;
                    booking.CheckOutDate = newCheckOut;
                    booking.NightsCount = newNights;
                    booking.ChildrenCountUntil6Years = dto.ChildrenCountUntil6Years;
                    booking.ChildrenCountUntil12Years = dto.ChildrenCountUntil12Years;
                    booking.TotalChildrenCount = dto.ChildrenCountUntil6Years + dto.ChildrenCountUntil12Years;

                    await _db.BookingRooms
                        .Where(x => x.BookingId == booking.BookingID)
                        .ExecuteDeleteAsync();

                    foreach (var room in newRooms)
                    {
                        _db.BookingRooms.Add(new BookingRoom
                        {
                            BookingRoomId = Guid.NewGuid(),
                            BookingId = booking.BookingID,
                            RoomType = room.RoomType,
                            Count = room.Count,
                            NightPrice = room.NightPrice
                        });
                    }

                    booking.NumberOfRooms = newRooms.Sum(x => x.Count);
                    booking.RoomType = newRooms.FirstOrDefault()?.RoomType ?? booking.RoomType;
                    booking.HotelNightPrice = newRooms.FirstOrDefault()?.NightPrice ?? 0;
                    booking.HotelTotal = newRooms.Sum(x => x.Count * x.NightPrice * newNights);
                }

                if (isAdmin)
                {
                    AddChange(audit, nameof(booking.Discount), booking.Discount, dto.Discount);
                    booking.Discount = dto.Discount;
                }

                if (booking.HasTransportation && dto.Transportation != null)
                {
                    await ApplyAllowedTransportationEditAsync(booking, dto.Transportation, audit);
                }

                var newPhones = NormalizePhones(dto.PhoneNumbers).ToList();
                AddChange(audit, "PhoneNumbers", oldPhones, FormatPhones(newPhones));

                await _db.Telephones
                    .Where(x => x.BookingID == booking.BookingID)
                    .ExecuteDeleteAsync();

                foreach (var phone in newPhones)
                {
                    _db.Telephones.Add(new Telephones
                    {
                        Id = Guid.NewGuid(),
                        BookingID = booking.BookingID,
                        PhoneNumber = phone.PhoneNumber,
                        Prime = phone.Prime
                    });
                }

                booking.SeatsCount = booking.TransportationSeats.Count;
                booking.SeatPrice = booking.TransportationSeats.FirstOrDefault()?.SeatPrice ?? 0;
                booking.TransportationTotal = booking.TransportationSeats.Sum(x => x.SeatPrice);

                booking.PaidAmount = booking.Payments.Sum(x => x.Amount);
                booking.GrandTotal = Math.Max(0, booking.HotelTotal + booking.TransportationTotal - booking.Discount);
                booking.RemainingAmount = Math.Max(0, booking.GrandTotal - booking.PaidAmount);
                booking.UpdatedAtUtc = DateTime.UtcNow;

                AddChange(audit, nameof(booking.HotelTotal), oldHotelTotal, booking.HotelTotal);
                AddChange(audit, "TripType", oldTripType, GetCurrentTripType(booking).ToString());
                AddChange(audit, nameof(booking.GrandTotal), oldGrandTotal, booking.GrandTotal);
                AddChange(audit, nameof(booking.RemainingAmount), oldRemaining, booking.RemainingAmount);

                if (audit.Details.Any())
                    _db.BookingAudits.Add(audit);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<string>.Ok(
                    booking.Code,
                    audit.Details.Any() ? "Booking updated successfully." : "No changes detected.");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return ResponceApi<string>.Fail("Booking data was changed or deleted. Please reload the page and try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<string>.Fail("Failed to update booking.", ex.Message);
            }
        }

        public async Task<ResponceApi<string>> AddPaymentAsync(AddBookingPaymentDTO dto, Guid currentUserId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (dto.BookingId == Guid.Empty)
                    return ResponceApi<string>.Fail("Booking id is required.");

                if (dto.Amount <= 0)
                    return ResponceApi<string>.Fail("Payment amount must be greater than zero.");

                var booking = await _db.Bookings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.BookingID == dto.BookingId && !x.IsDeleted);

                if (booking == null)
                    return ResponceApi<string>.Fail("Booking not found.");

                var paidBefore = await _db.BookingPayments
                    .Where(x => x.BookingId == dto.BookingId)
                    .SumAsync(x => (decimal?)x.Amount) ?? 0;

                var actualGrandTotal = Math.Max(0, booking.HotelTotal + booking.TransportationTotal - booking.Discount);

                var remainingBefore = Math.Max(0, actualGrandTotal - paidBefore);

                if (remainingBefore <= 0)
                    return ResponceApi<string>.Fail("This booking is already fully paid.");

                if (dto.Amount > remainingBefore)
                    return ResponceApi<string>.Fail(
                        $"Payment amount cannot be greater than remaining amount ({remainingBefore:N2}).");

                var payment = new BookingPayment
                {
                    PaymentId = Guid.NewGuid(),
                    BookingId = booking.BookingID,
                    Amount = dto.Amount,
                    PayType = dto.PayType,
                    PaidAtUtc = dto.PaidAt.Date.ToUniversalTime(),
                    Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
                };

                _db.BookingPayments.Add(payment);

                var paidAfter = paidBefore + dto.Amount;
                var remainingAfter = Math.Max(0, actualGrandTotal - paidAfter);

                var audit = CreateAudit(
                    booking.BookingID,
                    currentUserId,
                    "AddPayment",
                    $"Payment added: {dto.Amount:N2}.");

                AddChange(audit, "PaidAmount", paidBefore, paidAfter);
                AddChange(audit, "RemainingAmount", remainingBefore, remainingAfter);

                _db.BookingAudits.Add(audit);

                await _db.SaveChangesAsync();

                var affectedRows = await _db.Bookings
                    .Where(x => x.BookingID == dto.BookingId && !x.IsDeleted)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.GrandTotal, actualGrandTotal)
                        .SetProperty(x => x.PaidAmount, paidAfter)
                        .SetProperty(x => x.RemainingAmount, remainingAfter)
                        .SetProperty(x => x.UpdatedAtUtc, DateTime.UtcNow));

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync();
                    return ResponceApi<string>.Fail("Booking was modified or deleted. Please reload and try again.");
                }

                await transaction.CommitAsync();

                return ResponceApi<string>.Ok(booking.Code, "Payment added successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<string>.Fail("Failed to add payment.", ex.Message);
            }
        }

        public async Task<ResponceApi<string>> SoftDeleteAsync(Guid bookingId, Guid currentUserId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var booking = await _db.Bookings
                    .Include(b => b.TransportationSeats)
                    .FirstOrDefaultAsync(b => b.BookingID == bookingId && !b.IsDeleted);

                if (booking == null)
                    return ResponceApi<string>.Fail("Booking not found.");

                var transferSnapshots = await _db.CompanySeatBookings
                    .Where(x =>
                        x.TransferredFromBookingId == bookingId ||
                        x.TransferredFromBookingId == booking.BookingID)
                    .ToListAsync();

                if (transferSnapshots.Any())
                    _db.CompanySeatBookings.RemoveRange(transferSnapshots);

                var bookingTransportationSeats = await _db.BookingTransportationSeats
                    .Where(x => x.BookingId == bookingId)
                    .ToListAsync();

                if (bookingTransportationSeats.Any())
                    _db.BookingTransportationSeats.RemoveRange(bookingTransportationSeats);

                booking.IsDeleted = true;
                booking.DeletedAtUtc = DateTime.UtcNow;
                booking.DeletedByUserId = currentUserId;
                booking.UpdatedAtUtc = DateTime.UtcNow;
                booking.HasTransportation = false;
                booking.SeatsCount = 0;
                booking.SeatPrice = 0;
                booking.TransportationTotal = 0;

                var audit = CreateAudit(
                    booking.BookingID,
                    currentUserId,
                    "Delete",
                    $"Booking soft deleted by admin. Removed {transferSnapshots.Count} transfer snapshot(s) and {bookingTransportationSeats.Count} transportation seat(s).");

                AddChange(audit, "IsDeleted", false, true);

                if (transferSnapshots.Any())
                    AddChange(audit, "TransferSnapshotsRemoved", transferSnapshots.Count, 0);

                if (bookingTransportationSeats.Any())
                    AddChange(audit, "BookingTransportationSeatsRemoved", bookingTransportationSeats.Count, 0);

                _db.BookingAudits.Add(audit);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<string>.Ok(
                    booking.Code,
                    $"Booking deleted successfully. Removed {transferSnapshots.Count} transfer snapshot(s) and {bookingTransportationSeats.Count} transportation seat(s).");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<string>.Fail("Failed to delete booking.", ex.Message);
            }
        }

        public async Task<ResponceApi<BusLayoutResponseDTO>> GetBusLayoutAsync(Guid? busId, Guid? tripId, DateTime? date, TripDirection? direction)
        {
            Bus? bus;
            BusTrip? trip = null;

            if (tripId.HasValue && tripId.Value != Guid.Empty)
            {
                trip = await _db.BusTrips
                    .Include(x => x.Bus).ThenInclude(x => x.Seats)
                    .Include(x => x.ReservedSeats)
                    .FirstOrDefaultAsync(x => x.TripId == tripId.Value && !x.IsClosed);

                if (trip == null)
                    return ResponceApi<BusLayoutResponseDTO>.Fail("Trip not found.");

                bus = trip.Bus;
            }
            else
            {
                if (!busId.HasValue || busId.Value == Guid.Empty)
                    return ResponceApi<BusLayoutResponseDTO>.Fail("Bus is required.");

                bus = await _db.Buses
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(x => x.BusId == busId.Value && x.IsActive);

                if (bus == null)
                    return ResponceApi<BusLayoutResponseDTO>.Fail("Bus not found.");

                if (date.HasValue && direction.HasValue)
                {
                    var d = date.Value.Date;

                    trip = await _db.BusTrips
                        .Include(x => x.Bus).ThenInclude(x => x.Seats)
                        .Include(x => x.ReservedSeats)
                        .FirstOrDefaultAsync(x =>
                            x.BusId == bus.BusId &&
                            x.TripDate.Date == d &&
                            x.Direction == direction.Value &&
                            !x.IsClosed);
                }
            }

            var clientReservedIds = trip?.ReservedSeats
                .Select(x => x.SeatId)
                .ToHashSet() ?? new HashSet<Guid>();

            var companyReservedIds = trip != null
                ? (await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Where(x => x.TripId == trip.TripId && x.SeatId != null)
                    .Select(x => x.SeatId!.Value)
                    .ToListAsync())
                    .ToHashSet()
                : new HashSet<Guid>();

            var allReservedIds = clientReservedIds
                .Union(companyReservedIds)
                .ToHashSet();

            List<BusLayoutSeatResponseDTO> seats;

            if (trip != null && !string.IsNullOrWhiteSpace(trip.SeatsSnapshotJson))
            {
                List<TripSeatSnapshotDTO> snapshot;

                try
                {
                    snapshot = System.Text.Json.JsonSerializer.Deserialize<List<TripSeatSnapshotDTO>>(
                        trip.SeatsSnapshotJson,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<TripSeatSnapshotDTO>();
                }
                catch
                {
                    snapshot = new List<TripSeatSnapshotDTO>();
                }

                if (snapshot.Any())
                {
                    seats = snapshot
                        .OrderBy(x => x.RowNumber)
                        .ThenBy(x => x.ColumnNumber)
                        .Select(x => new BusLayoutSeatResponseDTO
                        {
                            SeatId = x.SeatId,
                            SeatNumber = x.SeatNumber,
                            Label = string.IsNullOrWhiteSpace(x.SeatLabel)
                                ? x.SeatNumber.ToString()
                                : x.SeatLabel,
                            Row = x.RowNumber,
                            Column = x.ColumnNumber,
                            Type = x.SeatType.ToString(),
                            IsActive = x.IsActive,
                            IsReserved = allReservedIds.Contains(x.SeatId)
                        })
                        .ToList();
                }
                else
                {
                    seats = bus.Seats
                        .OrderBy(x => x.RowNumber)
                        .ThenBy(x => x.ColumnNumber)
                        .Select(x => new BusLayoutSeatResponseDTO
                        {
                            SeatId = x.SeatId,
                            SeatNumber = x.SeatNumber,
                            Label = string.IsNullOrWhiteSpace(x.SeatLabel)
                                ? x.SeatNumber.ToString()
                                : x.SeatLabel,
                            Row = x.RowNumber,
                            Column = x.ColumnNumber,
                            Type = x.SeatType.ToString(),
                            IsActive = x.IsActive,
                            IsReserved = allReservedIds.Contains(x.SeatId)
                        })
                        .ToList();
                }
            }
            else
            {
                seats = bus.Seats
                    .OrderBy(x => x.RowNumber)
                    .ThenBy(x => x.ColumnNumber)
                    .Select(x => new BusLayoutSeatResponseDTO
                    {
                        SeatId = x.SeatId,
                        SeatNumber = x.SeatNumber,
                        Label = string.IsNullOrWhiteSpace(x.SeatLabel)
                            ? x.SeatNumber.ToString()
                            : x.SeatLabel,
                        Row = x.RowNumber,
                        Column = x.ColumnNumber,
                        Type = x.SeatType.ToString(),
                        IsActive = x.IsActive,
                        IsReserved = allReservedIds.Contains(x.SeatId)
                    })
                    .ToList();
            }

            return ResponceApi<BusLayoutResponseDTO>.Ok(new BusLayoutResponseDTO
            {
                TripId = trip?.TripId,
                BusId = trip?.BusId ?? bus.BusId,

                BusName = trip != null && !string.IsNullOrWhiteSpace(trip.BusNameSnapshot)
                    ? trip.BusNameSnapshot
                    : bus.Name,

                PlateNumber = trip != null && !string.IsNullOrWhiteSpace(trip.PlateNumberSnapshot)
                    ? trip.PlateNumberSnapshot
                    : bus.PlateNumber,

                Rows = trip != null && trip.LayoutRows > 0
                    ? trip.LayoutRows
                    : bus.LayoutRows,

                Columns = trip != null && trip.LayoutColumns > 0
                    ? trip.LayoutColumns
                    : bus.LayoutColumns,

                LayoutJson = trip != null && !string.IsNullOrWhiteSpace(trip.LayoutJson)
                    ? trip.LayoutJson
                    : bus.LayoutJson,

                Seats = seats
            });
        }

        public async Task<ResponceApi<List<Guid>>> GetReservedSeatsAsync(Guid tripId)
        {
            var reserved = await _db.BookingTransportationSeats.AsNoTracking().Where(x => x.TripId == tripId).Select(x => x.SeatId).ToListAsync();
            return ResponceApi<List<Guid>>.Ok(reserved);
        }

        public async Task<ResponceApi<List<AvailableTripDTO>>> GetAvailableTripsAsync(DateTime date, TripDirection direction)
        {
            var d = date.Date;

            var trips = await _db.BusTrips
                .AsNoTracking()
                .Include(x => x.Bus).ThenInclude(x => x.Seats)
                .Include(x => x.ReservedSeats)
                .Where(x => x.TripDate.Date == d && x.Direction == direction && !x.IsClosed)
                .OrderBy(x => x.Bus.Name)
                .ToListAsync();

            if (!trips.Any())
                return ResponceApi<List<AvailableTripDTO>>.Ok(new List<AvailableTripDTO>());

            var tripIds = trips.Select(x => x.TripId).ToList();

            // حجوزات الشركات — نعد بس الـ Inbound (اللي عندها SeatId)
            var companyBookingCounts = await _db.CompanySeatBookings
                .AsNoTracking()
                .Where(x => x.TripId != null &&
                            tripIds.Contains(x.TripId!.Value) &&
                            x.SeatId != null)
                .GroupBy(x => x.TripId!.Value)
                .Select(g => new { TripId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TripId, x => x.Count);

            var result = trips
                .Select(x =>
                {
                    var seatsCount = GetTripSeatsCount(x);
                    var clientReserved = x.ReservedSeats.Count;
                    var companyReserved = companyBookingCounts.TryGetValue(x.TripId, out var cc) ? cc : 0;
                    var availableCount = seatsCount - clientReserved - companyReserved;

                    return new AvailableTripDTO
                    {
                        TripId = x.TripId,
                        BusId = x.BusId,
                        BusName = !string.IsNullOrWhiteSpace(x.BusNameSnapshot) ? x.BusNameSnapshot : x.Bus.Name,
                        PlateNumber = !string.IsNullOrWhiteSpace(x.PlateNumberSnapshot) ? x.PlateNumberSnapshot : x.Bus.PlateNumber ?? string.Empty,
                        FromLocation = x.FromLocation ?? string.Empty,
                        ToLocation = x.ToLocation ?? string.Empty,
                        SeatsCount = seatsCount,
                        ReservedCount = clientReserved + companyReserved,
                        AvailableCount = Math.Max(0, availableCount)
                    };
                })
                .Where(x => x.AvailableCount > 0)
                .ToList();

            return ResponceApi<List<AvailableTripDTO>>.Ok(result);
        }

        private static int GetTripSeatsCount(BusTrip trip)
        {
            if (!string.IsNullOrWhiteSpace(trip.SeatsSnapshotJson))
            {
                try
                {
                    var snapshot = System.Text.Json.JsonSerializer.Deserialize<List<TripSeatSnapshotDTO>>(
                        trip.SeatsSnapshotJson,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (snapshot != null)
                        return snapshot.Count(x =>
                            x.IsActive &&
                            (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP));
                }
                catch { }
            }

            return trip.Bus?.Seats?.Count(x =>
                x.IsActive &&
                (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP)) ?? 0;
        }

        public async Task<ResponceApi<List<string>>> GetRouteToLocationsAsync(string fromLocation, TripDirection? direction = null)
        {
            if (string.IsNullOrWhiteSpace(fromLocation))
                return ResponceApi<List<string>>.Ok(new());

            var fromLower = fromLocation.Trim().ToLower();
            IQueryable<Bus> query = _db.Buses.AsNoTracking().Where(x => x.IsActive && x.FromLocation != null && x.ToLocation != null);

            var data = direction == TripDirection.Return
                ? await query.Where(x => x.ToLocation!.Trim().ToLower() == fromLower).Select(x => x.FromLocation!.Trim()).Distinct().OrderBy(x => x).ToListAsync()
                : await query.Where(x => x.FromLocation!.Trim().ToLower() == fromLower).Select(x => x.ToLocation!.Trim()).Distinct().OrderBy(x => x).ToListAsync();

            return ResponceApi<List<string>>.Ok(data);
        }

        public async Task<ResponceApi<List<RouteBusOptionDTO>>> GetRouteBusesAsync(string fromLocation, string toLocation, TripDirection? direction = null)
        {
            if (string.IsNullOrWhiteSpace(fromLocation) || string.IsNullOrWhiteSpace(toLocation))
                return ResponceApi<List<RouteBusOptionDTO>>.Ok(new());

            fromLocation = fromLocation.Trim();
            toLocation = toLocation.Trim();
            var fromLower = fromLocation.ToLower();
            var toLower = toLocation.ToLower();

            var query = _db.Buses.AsNoTracking().Where(x => x.IsActive && x.FromLocation != null && x.ToLocation != null);
            query = direction == TripDirection.Return
                ? query.Where(x => x.FromLocation!.Trim().ToLower() == toLower && x.ToLocation!.Trim().ToLower() == fromLower)
                : query.Where(x => x.FromLocation!.Trim().ToLower() == fromLower && x.ToLocation!.Trim().ToLower() == toLower);

            var data = await query.OrderBy(x => x.Name).Select(x => new RouteBusOptionDTO
            {
                BusId = x.BusId,
                BusName = x.Name,
                PlateNumber = x.PlateNumber ?? string.Empty,
                FromLocation = fromLocation,
                ToLocation = toLocation,
                OriginalFromLocation = x.FromLocation ?? string.Empty,
                OriginalToLocation = x.ToLocation ?? string.Empty
            }).ToListAsync();

            return ResponceApi<List<RouteBusOptionDTO>>.Ok(data);
        }

        public async Task<ResponceApi<List<RouteBusAvailabilityDTO>>> GetRouteBusesBySeatCount(string fromLocation, string toLocation, TripDirection direction, DateTime tripDate, int requiredSeats)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (string.IsNullOrWhiteSpace(fromLocation) || string.IsNullOrWhiteSpace(toLocation))
                    return ResponceApi<List<RouteBusAvailabilityDTO>>.Ok(new());

                if (requiredSeats < 1)
                    return ResponceApi<List<RouteBusAvailabilityDTO>>.Fail("Required seats count must be at least 1.");

                fromLocation = fromLocation.Trim();
                toLocation = toLocation.Trim();

                var fromLower = fromLocation.ToLower();
                var toLower = toLocation.ToLower();
                var date = tripDate.Date;

                // 1) الأول نشوف هل فيه Trips في اليوم ده على نفس route والاتجاه
                // هنا لا نعتمد على IsActive للـ Bus لأن الـ Trip يعتبر Snapshot
                var trips = await _db.BusTrips
                    .Include(x => x.Bus)
                        .ThenInclude(x => x.Seats)
                    .Include(x => x.ReservedSeats)
                    .Where(x =>
                        x.Direction == direction &&
                        x.TripDate.Date == date &&
                        !x.IsClosed &&
                        x.Bus.FromLocation != null &&
                        x.Bus.ToLocation != null)
                    .ToListAsync();

                trips = trips.Where(x =>
                {
                    var busFrom = x.Bus.FromLocation!.Trim().ToLower();
                    var busTo = x.Bus.ToLocation!.Trim().ToLower();

                    return direction == TripDirection.Return
                        ? busFrom == toLower && busTo == fromLower
                        : busFrom == fromLower && busTo == toLower;
                }).ToList();

                List<Bus> buses;

                if (trips.Any())
                {
                    // فيه Trips في اليوم ده:
                    // نعرض الـ buses الخاصة بالـ trips حتى لو الـ Bus بقى Inactive
                    buses = trips
                        .Select(x => x.Bus)
                        .GroupBy(x => x.BusId)
                        .Select(g => g.First())
                        .OrderBy(x => x.Name)
                        .ToList();
                }
                else
                {
                    // مفيش أي Trip في اليوم ده:
                    // نجيب فقط الـ Active buses المتاحة للـ route
                    buses = await GetRouteBusesQuery(fromLocation, toLocation, direction)
                        .Include(x => x.Seats)
                        .OrderBy(x => x.Name)
                        .ToListAsync();

                    if (!buses.Any())
                        return ResponceApi<List<RouteBusAvailabilityDTO>>.Fail("No active buses found for this route.");
                }

                var busesWithCapacity = buses
                    .Select(bus => new { Bus = bus, Capacity = CountSelectableSeats(bus.Seats) })
                    .Where(x => x.Capacity > 0)
                    .OrderBy(x => x.Capacity)
                    .ThenBy(x => x.Bus.Name)
                    .ToList();

                var maxCapacity = busesWithCapacity.Select(x => x.Capacity).DefaultIfEmpty(0).Max();

                if (requiredSeats > maxCapacity)
                    return ResponceApi<List<RouteBusAvailabilityDTO>>.Fail(
                        $"Not enough buses for this route. Required: {requiredSeats}, max available: {maxCapacity}.");

                // حجوزات الشركات — Inbound فقط (SeatId != null)
                var tripIdsInDate = trips.Select(x => x.TripId).ToList();

                var companyBookingCounts = await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Where(x => x.TripId != null &&
                                tripIdsInDate.Contains(x.TripId!.Value) &&
                                x.SeatId != null)
                    .GroupBy(x => x.TripId!.Value)
                    .Select(g => new { TripId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TripId, x => x.Count);

                // حذف الـ Trips الفاضية
                var emptyTrips = trips.Where(x =>
                    !x.ReservedSeats.Any() &&
                    (!companyBookingCounts.TryGetValue(x.TripId, out var cc) || cc == 0)).ToList();

                if (emptyTrips.Any())
                {
                    _db.BusTrips.RemoveRange(emptyTrips);
                    await _db.SaveChangesAsync();
                    trips = trips.Except(emptyTrips).ToList();
                }

                var result = BuildAvailabilityResult(
                    busesWithCapacity.Select(x => (x.Bus, x.Capacity)).ToList(),
                    trips,
                    companyBookingCounts,
                    fromLocation, toLocation, requiredSeats);

                if (!result.Any())
                {
                    var bestBus = busesWithCapacity.First(x => x.Capacity >= requiredSeats);

                    var newTrip = CreateTrip(bestBus.Bus, direction, date, fromLocation, toLocation);

                    _db.BusTrips.Add(newTrip);
                    await _db.SaveChangesAsync();

                    result.Add(new RouteBusAvailabilityDTO
                    {
                        BusId = newTrip.BusId,
                        BusName = newTrip.BusNameSnapshot,
                        PlateNumber = newTrip.PlateNumberSnapshot ?? string.Empty,
                        FromLocation = newTrip.FromLocation ?? fromLocation,
                        ToLocation = newTrip.ToLocation ?? toLocation,
                        TripId = newTrip.TripId,
                        CreateNewTrip = false,
                        SeatsCount = newTrip.SeatsCountSnapshot,
                        ReservedCount = 0,
                        AvailableCount = newTrip.SeatsCountSnapshot
                    });
                }

                await transaction.CommitAsync();

                return ResponceApi<List<RouteBusAvailabilityDTO>>.Ok(
                    result.OrderBy(x => x.AvailableCount).ThenBy(x => x.BusName).ToList(),
                    "Route buses prepared successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<List<RouteBusAvailabilityDTO>>.Fail("Failed to prepare route buses.", ex.Message);
            }
        }

        private IQueryable<BookingData> BuildDetailsQuery()
        {
            return _db.Bookings.AsNoTracking()
                .Include(b => b.User)
                .Include(b => b.PhoneNumbers)
                .Include(b => b.Rooms)
                .Include(b => b.Payments)
                .Include(b => b.TransportationSeats)
                    .ThenInclude(x => x.Trip)
                        .ThenInclude(x => x.Bus)
                            .ThenInclude(x => x.Seats)
                .Include(b => b.AuditLogs)
                    .ThenInclude(a => a.ChangedByUser)
                .Include(b => b.AuditLogs)
                    .ThenInclude(a => a.Details);
        }

        private IQueryable<Bus> GetRouteBusesQuery(string fromLocation, string toLocation, TripDirection direction)
        {
            var fromLower = fromLocation.Trim().ToLower();
            var toLower = toLocation.Trim().ToLower();

            var query = _db.Buses.Where(x => x.IsActive && x.FromLocation != null && x.ToLocation != null);

            return direction == TripDirection.Return
                ? query.Where(x => x.FromLocation!.Trim().ToLower() == toLower && x.ToLocation!.Trim().ToLower() == fromLower)
                : query.Where(x => x.FromLocation!.Trim().ToLower() == fromLower && x.ToLocation!.Trim().ToLower() == toLower);
        }

        private static int CountSelectableSeats(IEnumerable<BusSeat> seats)
            => seats.Count(s => s.IsActive && (s.SeatType == SeatType.Normal || s.SeatType == SeatType.VIP));

        private static List<RouteBusAvailabilityDTO> BuildAvailabilityResult(List<(Bus Bus, int Capacity)> buses, List<BusTrip> trips, Dictionary<Guid, int> companyBookingCounts, string fromLocation, string toLocation, int requiredSeats)
        {
            var result = new List<RouteBusAvailabilityDTO>();

            foreach (var item in buses)
            {
                foreach (var trip in trips.Where(x => x.BusId == item.Bus.BusId))
                {
                    var seatsCount = GetTripSeatsCount(trip);
                    var clientReserved = trip.ReservedSeats.Select(x => x.SeatId).Distinct().Count();
                    var companyReserved = companyBookingCounts.TryGetValue(trip.TripId, out var cc) ? cc : 0;
                    var totalReserved = clientReserved + companyReserved;
                    var availableCount = seatsCount - totalReserved;

                    if (availableCount < requiredSeats)
                        continue;

                    result.Add(new RouteBusAvailabilityDTO
                    {
                        BusId = trip.BusId,
                        BusName = !string.IsNullOrWhiteSpace(trip.BusNameSnapshot)
                            ? trip.BusNameSnapshot
                            : item.Bus.Name,
                        PlateNumber = !string.IsNullOrWhiteSpace(trip.PlateNumberSnapshot)
                            ? trip.PlateNumberSnapshot
                            : item.Bus.PlateNumber ?? string.Empty,
                        FromLocation = trip.FromLocation ?? fromLocation,
                        ToLocation = trip.ToLocation ?? toLocation,
                        TripId = trip.TripId,
                        CreateNewTrip = false,
                        SeatsCount = seatsCount,
                        ReservedCount = totalReserved,
                        AvailableCount = Math.Max(0, availableCount)
                    });
                }
            }

            return result;
        }

        private static BusTrip CreateTrip(Bus bus, TripDirection direction, DateTime date, string fromLocation, string toLocation)
        {
            var seatsSnapshot = bus.Seats
                .OrderBy(x => x.RowNumber)
                .ThenBy(x => x.ColumnNumber)
                .Select(x => new TripSeatSnapshotDTO
                {
                    SeatId = x.SeatId,
                    SeatNumber = x.SeatNumber,
                    SeatLabel = x.SeatLabel,
                    SeatType = x.SeatType,
                    RowNumber = x.RowNumber,
                    ColumnNumber = x.ColumnNumber,
                    IsActive = x.IsActive
                })
                .ToList();

            var passengerSeatsCount = seatsSnapshot.Count(x =>
                x.IsActive &&
                (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP));

            return new BusTrip
            {
                TripId = Guid.NewGuid(),
                BusId = bus.BusId,
                Direction = direction,
                TripDate = date.Date,

                FromLocation = string.IsNullOrWhiteSpace(fromLocation)
                    ? bus.FromLocation
                    : fromLocation.Trim(),

                ToLocation = string.IsNullOrWhiteSpace(toLocation)
                    ? bus.ToLocation
                    : toLocation.Trim(),

                BusNameSnapshot = bus.Name,
                PlateNumberSnapshot = bus.PlateNumber,
                SeatsCountSnapshot = passengerSeatsCount,

                LayoutRows = bus.LayoutRows,
                LayoutColumns = bus.LayoutColumns,
                LayoutJson = bus.LayoutJson,
                SeatsSnapshotJson = System.Text.Json.JsonSerializer.Serialize(seatsSnapshot),

                IsLayoutCustomized = false,
                IsClosed = false
            };
        }

        private async Task<BusTrip> GetOrCreateTripAsync(Guid? tripId, Guid? busId, TripDirection direction, DateTime tripDate, bool createNewTrip, string? fromLocation, string? toLocation)
        {
            tripDate = tripDate.Date;

            if (!createNewTrip && tripId.HasValue && tripId.Value != Guid.Empty)
            {
                var existingTrip = await _db.BusTrips
                    .Include(x => x.Bus)
                    .Include(x => x.ReservedSeats)
                    .FirstOrDefaultAsync(x =>
                        x.TripId == tripId.Value &&
                        x.Direction == direction &&
                        x.TripDate.Date == tripDate &&
                        !x.IsClosed);

                if (existingTrip == null)
                    throw new InvalidOperationException("Selected trip not found or closed.");

                return existingTrip;
            }

            if (!busId.HasValue || busId.Value == Guid.Empty)
                throw new InvalidOperationException("Selected bus is required.");

            // ← مهم: Include Seats عشان CreateTrip يعمل Snapshot صح
            var bus = await _db.Buses
                .Include(x => x.Seats)
                .FirstOrDefaultAsync(x => x.BusId == busId.Value && x.IsActive);

            if (bus == null)
                throw new InvalidOperationException("Selected bus not found or inactive.");

            var trip = CreateTrip(
                bus, direction, tripDate,
                fromLocation?.Trim() ?? string.Empty,
                toLocation?.Trim() ?? string.Empty);

            _db.BusTrips.Add(trip);
            return trip;
        }

        private async Task<bool> AreSeatsAvailableAsync(Guid tripId, IEnumerable<Guid> seatIds, Guid? ignoreBookingId = null)
        {
            var ids = seatIds.Where(x => x != Guid.Empty).Distinct().ToList();
            if (ids.Count == 0) return false;

            var clientConflict = await _db.BookingTransportationSeats.AnyAsync(x =>
                x.TripId == tripId &&
                ids.Contains(x.SeatId) &&
                (!ignoreBookingId.HasValue || x.BookingId != ignoreBookingId.Value));

            if (clientConflict) return false;

            var companyConflict = await _db.CompanySeatBookings.AnyAsync(x =>
                x.TripId == tripId &&
                x.SeatId != null &&
                ids.Contains(x.SeatId!.Value));

            return !companyConflict;
        }

        private static void CalculateTotals(CreateBookingDTO dto)
        {
            dto.Rooms ??= new List<BookingRoomDTO>();
            dto.Payments ??= new List<BookingPaymentDTO>();
            dto.TransportationSeats ??= new List<BookingSeatDTO>();
            dto.Transportation ??= new TransportationBookingDTO();
            dto.Transportation.DepartureSeats ??= new List<BookingSeatDTO>();
            dto.Transportation.ReturnSeats ??= new List<BookingSeatDTO>();

            // ── Hotel Total ─────────────────────────────
            if (dto.HasHotel)
            {
                dto.NightsCount = Math.Max(1, (dto.CheckOutDate.Date - dto.CheckInDate.Date).Days);

                dto.Rooms = dto.Rooms
                    .Where(x => x.Count > 0)
                    .ToList();

                dto.HotelTotal = dto.Rooms.Sum(x => x.Count * x.NightPrice * dto.NightsCount);
                dto.NumberOfRooms = dto.Rooms.Sum(x => x.Count);
                dto.RoomType = dto.Rooms.FirstOrDefault()?.RoomType ?? dto.RoomType;
                dto.HotelNightPrice = dto.Rooms.FirstOrDefault()?.NightPrice ?? 0;
            }
            else
            {
                dto.Rooms.Clear();
                dto.NumberOfRooms = 0;
                dto.RoomType = dto.RoomType;
                dto.HotelNightPrice = 0;
                dto.NightsCount = 0;
                dto.HotelTotal = 0;
            }

            // ── Transportation Total ────────────────────
            if (dto.HasTransportation)
            {
                var allTransportationSeats = new List<BookingSeatDTO>();

                if (dto.Transportation.TripType == TransportationTripType.Departure ||
                    dto.Transportation.TripType == TransportationTripType.RoundTrip)
                {
                    allTransportationSeats.AddRange(
                        dto.Transportation.DepartureSeats
                            .Where(x => x.SeatId != Guid.Empty));
                }

                if (dto.Transportation.TripType == TransportationTripType.Return ||
                    dto.Transportation.TripType == TransportationTripType.RoundTrip)
                {
                    allTransportationSeats.AddRange(
                        dto.Transportation.ReturnSeats
                            .Where(x => x.SeatId != Guid.Empty));
                }

                dto.TransportationSeats = allTransportationSeats;

                dto.SeatsCount = dto.TransportationSeats.Count;
                dto.TransportationTotal = dto.TransportationSeats.Sum(x => x.SeatPrice);
                dto.SeatPrice = dto.TransportationSeats.FirstOrDefault()?.SeatPrice ?? 0;
            }
            else
            {
                dto.TransportationSeats.Clear();
                dto.Transportation.DepartureSeats.Clear();
                dto.Transportation.ReturnSeats.Clear();

                dto.SeatsCount = 0;
                dto.SeatPrice = 0;
                dto.TransportationTotal = 0;
            }

            // ── Final Total + Payments ──────────────────
            dto.Discount = Math.Max(0, dto.Discount);

            dto.GrandTotal = Math.Max(
                0,
                dto.HotelTotal + dto.TransportationTotal - dto.Discount
            );

            dto.Payments = dto.Payments
                .Where(x => x.Amount > 0)
                .ToList();

            dto.PaidAmount = dto.Payments.Sum(x => x.Amount);

            dto.RemainingAmount = Math.Max(0, dto.GrandTotal - dto.PaidAmount);
        }

        private BookingListItemDTO ToListItem(BookingData b)
        {
            return new BookingListItemDTO
            {
                BookingID = b.BookingID,
                Code = b.Code,
                UserId = b.UserId,
                ClientName = b.ClientName,
                HotelName = b.HotelName,
                HasHotel = b.HasHotel,
                HasTransportation = b.HasTransportation,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                NumberOfRooms = b.Rooms.Any() ? b.Rooms.Sum(r => r.Count) : b.NumberOfRooms,
                RoomTypeName = b.Rooms.Any() ? string.Join(", ", b.Rooms.Select(r => $"{r.Count} {r.RoomType}")) : b.RoomType.ToString(),
                PayTypeName = b.Payments.Any() ? string.Join(", ", b.Payments.Select(p => p.PayType.ToString()).Distinct()) : b.PayType.ToString(),
                HotelTotal = b.HotelTotal,
                TransportationTotal = b.TransportationTotal,
                Discount = b.Discount,
                PaidAmount = b.PaidAmount,
                GrandTotal = b.GrandTotal,
                RemainingAmount = b.RemainingAmount,
                Notes = b.Notes,
                PhoneNumbersText = string.Join(", ", b.PhoneNumbers.OrderByDescending(p => p.Prime).Select(p => p.PhoneNumber)),
                CreatedBy = b.User.UserName,
                CreatedAtUtc = b.CreatedAtUtc
            };
        }

        private BookingDetailsDTO ToDetails(BookingData booking)
        {
            var dto = MapDetails(booking);

            var transportationSeats = booking.TransportationSeats
                .Where(x => x.Trip != null && x.Trip.Bus != null)
                .ToList();

            var activeSeatDtos = transportationSeats
                .Select(ToSeatDto)
                .OrderBy(x => x.Direction)
                .ThenBy(x => x.TripDate)
                .ThenBy(x => x.RowNumber)
                .ThenBy(x => x.ColumnNumber)
                .ToList();

            var transferredSeatDtos = _db.CompanySeatBookings
                .AsNoTracking()
                .Include(x => x.Company)
                .Where(x =>
                    x.BookingDirection == CompanySeatBookingDirection.Outbound &&
                    x.TransferredFromBookingId == booking.BookingID &&
                    !string.IsNullOrWhiteSpace(x.TransferredSeatsJson))
                .ToList()
                .SelectMany(BuildTransferredBookingSeatDtos)
                .ToList();

            dto.TransportationSeats = activeSeatDtos
                .Concat(transferredSeatDtos)
                .OrderBy(x => x.Direction)
                .ThenBy(x => x.TripDate)
                .ThenBy(x => x.RowNumber)
                .ThenBy(x => x.ColumnNumber)
                .ToList();

            dto.TransportationTrips = BuildDetailsTripsWithTransferredSeats(
                transportationSeats,
                transferredSeatDtos
            );

            return dto;
        }

        private static List<BookingSeatDTO> BuildTransferredBookingSeatDtos(CompanySeatBooking transfer)
        {
            var result = new List<BookingSeatDTO>();

            if (string.IsNullOrWhiteSpace(transfer.TransferredSeatsJson))
                return result;

            List<TransferredSeatSnapshot>? snapshots;

            try
            {
                snapshots = JsonSerializer.Deserialize<List<TransferredSeatSnapshot>>(
                    transfer.TransferredSeatsJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch
            {
                return result;
            }

            if (snapshots == null || snapshots.Count == 0)
                return result;

            foreach (var snap in snapshots)
            {
                result.Add(new BookingSeatDTO
                {
                    Id = snap.BookingSeatId,
                    Direction = Enum.IsDefined(typeof(TripDirection), snap.Direction)
                        ? (TripDirection)snap.Direction
                        : TripDirection.Departure,

                    TripId = snap.TripId,
                    SeatId = snap.SeatId,
                    SeatPrice = snap.SeatPrice,

                    SeatNumber = snap.SeatNumber,
                    SeatLabel = !string.IsNullOrWhiteSpace(snap.SeatLabel)
                        ? snap.SeatLabel
                        : snap.SeatNumber > 0
                            ? snap.SeatNumber.ToString()
                            : "?",

                    SeatType = snap.SeatType ?? SeatType.Normal,
                    RowNumber = snap.RowNumber,
                    ColumnNumber = snap.ColumnNumber,

                    TripDate = transfer.TripDate ?? DateTime.MinValue,
                    FromLocation = snap.FromLocation,
                    ToLocation = snap.ToLocation,

                    BusId = Guid.Empty,
                    BusName = "Transferred",
                    PlateNumber = string.Empty,

                    IsTransferredToCompany = true,
                    TransferredToCompanyName = transfer.Company?.Name,
                    TransferredToCompanyPhone = transfer.Company?.PhoneNumber,
                    CompanySeatBookingId = transfer.BookingId
                });
            }

            return result;
        }

        private List<BookingTransportationTripDTO> BuildDetailsTripsWithTransferredSeats(List<BookingTransportationSeat> activeSeats, List<BookingSeatDTO> transferredSeats)
        {
            var trips = activeSeats
                .GroupBy(x => x.TripId)
                .Select(ToTripDto)
                .ToList();

            foreach (var transferredGroup in transferredSeats.GroupBy(x => x.TripId))
            {
                var existingTrip = trips.FirstOrDefault(x => x.TripId == transferredGroup.Key);

                if (existingTrip != null)
                {
                    existingTrip.BookedSeats = existingTrip.BookedSeats
                        .Concat(transferredGroup)
                        .OrderBy(x => x.RowNumber)
                        .ThenBy(x => x.ColumnNumber)
                        .ToList();

                    existingTrip.Total = existingTrip.BookedSeats.Sum(x => x.SeatPrice);
                    continue;
                }

                var first = transferredGroup.First();

                var trip = _db.BusTrips
                    .AsNoTracking()
                    .Include(x => x.Bus)
                        .ThenInclude(x => x.Seats)
                    .FirstOrDefault(x => x.TripId == transferredGroup.Key);

                if (trip == null || trip.Bus == null)
                    continue;

                trips.Add(new BookingTransportationTripDTO
                {
                    TripId = trip.TripId,
                    Direction = trip.Direction,
                    TripDate = trip.TripDate,
                    FromLocation = trip.FromLocation ?? first.FromLocation,
                    ToLocation = trip.ToLocation ?? first.ToLocation,
                    BusId = trip.BusId,
                    BusName = !string.IsNullOrWhiteSpace(trip.BusNameSnapshot)
                        ? trip.BusNameSnapshot
                        : trip.Bus.Name,
                    PlateNumber = !string.IsNullOrWhiteSpace(trip.PlateNumberSnapshot)
                        ? trip.PlateNumberSnapshot
                        : trip.Bus.PlateNumber ?? string.Empty,
                    LayoutRows = trip.LayoutRows > 0 ? trip.LayoutRows : trip.Bus.LayoutRows,
                    LayoutColumns = trip.LayoutColumns > 0 ? trip.LayoutColumns : trip.Bus.LayoutColumns,
                    Total = transferredGroup.Sum(x => x.SeatPrice),
                    BusSeats = trip.Bus.Seats
                        .OrderBy(s => s.RowNumber)
                        .ThenBy(s => s.ColumnNumber)
                        .Select(s => new BookingBusSeatDTO
                        {
                            SeatId = s.SeatId,
                            SeatNumber = s.SeatNumber,
                            SeatLabel = string.IsNullOrWhiteSpace(s.SeatLabel)
                                ? s.SeatNumber.ToString()
                                : s.SeatLabel,
                            SeatType = s.SeatType,
                            RowNumber = s.RowNumber,
                            ColumnNumber = s.ColumnNumber,
                            IsActive = s.IsActive
                        })
                        .ToList(),
                    BookedSeats = transferredGroup
                        .OrderBy(x => x.RowNumber)
                        .ThenBy(x => x.ColumnNumber)
                        .ToList()
                });
            }

            return trips
                .OrderBy(x => x.Direction)
                .ThenBy(x => x.TripDate)
                .ToList();
        }

        private static BookingSeatDTO ToSeatDto(BookingTransportationSeat x)
        {
            var snapshotSeats = GetSeatsFromSnapshot(x.Trip);
            var snap = snapshotSeats.FirstOrDefault(s => s.SeatId == x.SeatId);

            return new BookingSeatDTO
            {
                Id = x.BookingSeatId,
                Direction = x.Direction,
                TripId = x.TripId,
                SeatId = x.SeatId,
                SeatPrice = x.SeatPrice,

                SeatNumber = snap?.SeatNumber ?? 0,
                SeatLabel = snap == null
                    ? "?"
                    : string.IsNullOrWhiteSpace(snap.SeatLabel)
                        ? snap.SeatNumber.ToString()
                        : snap.SeatLabel,

                SeatType = snap?.SeatType ?? SeatType.Normal,
                RowNumber = snap?.RowNumber ?? 0,
                ColumnNumber = snap?.ColumnNumber ?? 0,

                TripDate = x.Trip.TripDate,
                FromLocation = !string.IsNullOrWhiteSpace(x.FromLocation)
                    ? x.FromLocation
                    : x.Trip.FromLocation ?? string.Empty,
                ToLocation = !string.IsNullOrWhiteSpace(x.ToLocation)
                    ? x.ToLocation
                    : x.Trip.ToLocation ?? string.Empty,

                BusId = x.Trip.BusId,
                BusName = !string.IsNullOrWhiteSpace(x.Trip.BusNameSnapshot)
                    ? x.Trip.BusNameSnapshot
                    : x.Trip.Bus?.Name ?? string.Empty,

                PlateNumber = !string.IsNullOrWhiteSpace(x.Trip.PlateNumberSnapshot)
                    ? x.Trip.PlateNumberSnapshot
                    : x.Trip.Bus?.PlateNumber ?? string.Empty
            };
        }

        private static BookingTransportationTripDTO ToTripDto(IGrouping<Guid, BookingTransportationSeat> g)
        {
            var first = g.First();
            var trip = first.Trip;
            var bus = trip.Bus;

            var snapshotSeats = GetSeatsFromSnapshot(trip);

            var bookedSeats = g
                .Select(ToSeatDto)
                .OrderBy(x => x.RowNumber)
                .ThenBy(x => x.ColumnNumber)
                .ToList();

            return new BookingTransportationTripDTO
            {
                TripId = trip.TripId,
                Direction = trip.Direction,
                TripDate = trip.TripDate,
                FromLocation = trip.FromLocation ?? string.Empty,
                ToLocation = trip.ToLocation ?? string.Empty,

                BusId = trip.BusId,
                BusName = !string.IsNullOrWhiteSpace(trip.BusNameSnapshot)
                    ? trip.BusNameSnapshot
                    : bus?.Name ?? string.Empty,

                PlateNumber = !string.IsNullOrWhiteSpace(trip.PlateNumberSnapshot)
                    ? trip.PlateNumberSnapshot
                    : bus?.PlateNumber ?? string.Empty,

                LayoutRows = trip.LayoutRows > 0 ? trip.LayoutRows : bus?.LayoutRows ?? 0,
                LayoutColumns = trip.LayoutColumns > 0 ? trip.LayoutColumns : bus?.LayoutColumns ?? 0,

                Total = g.Sum(x => x.SeatPrice),

                BusSeats = snapshotSeats
                    .OrderBy(s => s.RowNumber)
                    .ThenBy(s => s.ColumnNumber)
                    .Select(s => new BookingBusSeatDTO
                    {
                        SeatId = s.SeatId,
                        SeatNumber = s.SeatNumber,
                        SeatLabel = string.IsNullOrWhiteSpace(s.SeatLabel)
                            ? s.SeatNumber.ToString()
                            : s.SeatLabel,
                        SeatType = s.SeatType,
                        RowNumber = s.RowNumber,
                        ColumnNumber = s.ColumnNumber,
                        IsActive = s.IsActive
                    })
                    .ToList(),

                BookedSeats = bookedSeats
            };
        }

        private static void AddPhones(BookingData booking, IEnumerable<BookingPhoneDTO> phones)
        {
            foreach (var phone in NormalizePhones(phones))
            {
                booking.PhoneNumbers.Add(new Telephones
                {
                    Id = Guid.NewGuid(),
                    BookingID = booking.BookingID,
                    PhoneNumber = phone.PhoneNumber,
                    Prime = phone.Prime
                });
            }
        }

        private static void AddRooms(BookingData booking, CreateBookingDTO dto)
        {
            if (!dto.HasHotel) return;
            foreach (var room in dto.Rooms.Where(x => x.Count > 0))
            {
                booking.Rooms.Add(new BookingRoom
                {
                    BookingRoomId = Guid.NewGuid(),
                    BookingId = booking.BookingID,
                    RoomType = room.RoomType,
                    Count = room.Count,
                    NightPrice = room.NightPrice
                });
            }
        }

        private static void AddPayments(BookingData booking, IEnumerable<BookingPaymentDTO> payments)
        {
            foreach (var payment in payments.Where(x => x.Amount > 0))
            {
                booking.Payments.Add(new BookingPayment
                {
                    PaymentId = Guid.NewGuid(),
                    BookingId = booking.BookingID,
                    Amount = payment.Amount,
                    PayType = payment.PayType,
                    PaidAtUtc = payment.PaidAt.ToUniversalTime(),
                    Notes = payment.Notes
                });
            }
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            for (var attempt = 0; attempt < 50; attempt++)
            {
                var code = GenerateCode();
                var exists = await _db.Bookings.AnyAsync(b => b.Code == code);
                if (!exists) return code;
            }
            throw new InvalidOperationException("Unable to generate a unique booking code.");
        }

        private static string GenerateCode()
        {
            Span<char> chars = stackalloc char[6];
            Span<byte> bytes = stackalloc byte[6];
            RandomNumberGenerator.Fill(bytes);
            for (var i = 0; i < chars.Length; i++) chars[i] = CodeChars[bytes[i] % CodeChars.Length];
            return new string(chars).ToLowerInvariant();
        }

        private static IEnumerable<BookingPhoneDTO> NormalizePhones(IEnumerable<BookingPhoneDTO>? phones)
        {
            var list = (phones ?? Enumerable.Empty<BookingPhoneDTO>())
                .Where(p => !string.IsNullOrWhiteSpace(p.PhoneNumber))
                .Select((p, index) => new BookingPhoneDTO { PhoneNumber = p.PhoneNumber.Trim(), Prime = p.Prime || index == 0 })
                .GroupBy(p => p.PhoneNumber)
                .Select(g => g.First())
                .ToList();

            if (list.Count > 0 && !list.Any(p => p.Prime)) list[0].Prime = true;
            if (list.Count(p => p.Prime) > 1)
            {
                var found = false;
                foreach (var p in list)
                {
                    if (p.Prime && !found) found = true;
                    else p.Prime = false;
                }
            }
            return list;
        }

        private static int GetRowPriority(BookingListItemDTO item, DateTime today)
        {
            if (item.CheckOutDate.Date < today) return 4;
            var daysToCheckIn = (item.CheckInDate.Date - today).Days;
            if (item.CheckInDate.Date == today || daysToCheckIn == 1) return 1;
            if (item.CheckInDate.Date > today) return 2;
            if (item.CheckInDate.Date <= today && item.CheckOutDate.Date >= today) return 3;
            return 5;
        }

        private static BookingAudit CreateAudit(Guid bookingId, Guid userId, string actionType, string notes)
        {
            return new BookingAudit
            {
                AuditId = Guid.NewGuid(),
                BookingId = bookingId,
                ChangedByUserId = userId,
                ChangedAtUtc = DateTime.UtcNow,
                ActionType = actionType,
                Notes = notes
            };
        }

        private static void AddChange(BookingAudit audit, string fieldName, object? oldValue, object? newValue)
        {
            var oldText = FormatValue(oldValue);
            var newText = FormatValue(newValue);
            if (string.Equals(oldText, newText, StringComparison.Ordinal)) return;
            audit.Details.Add(new BookingAuditDetail { DetailId = Guid.NewGuid(), AuditId = audit.AuditId, FieldName = fieldName, OldValue = oldText, NewValue = newText });
        }

        private static string? FormatValue(object? value)
        {
            return value switch
            {
                null => null,
                DateTime d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                decimal d => d.ToString("0.##", CultureInfo.InvariantCulture),
                bool b => b ? "Yes" : "No",
                _ => value.ToString()
            };
        }

        private static BookingDetailsDTO MapDetails(BookingData b)
        {
            return new BookingDetailsDTO
            {
                BookingID = b.BookingID,
                Code = b.Code,
                UserId = b.UserId,
                ClientName = b.ClientName,
                HotelName = b.HotelName,
                HasHotel = b.HasHotel,
                HasTransportation = b.HasTransportation,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                NumberOfRooms = b.Rooms.Any() ? b.Rooms.Sum(x => x.Count) : b.NumberOfRooms,
                RoomTypeName = b.Rooms.Any() ? string.Join(", ", b.Rooms.Select(x => $"{x.Count} {x.RoomType}")) : b.RoomType.ToString(),
                PayTypeName = b.Payments.Any() ? string.Join(", ", b.Payments.Select(x => x.PayType.ToString()).Distinct()) : b.PayType.ToString(),
                ChildrenCountUntil6Years = b.ChildrenCountUntil6Years,
                ChildrenCountUntil12Years = b.ChildrenCountUntil12Years,
                TotalChildrenCount = b.TotalChildrenCount,
                HotelNightPrice = b.HotelNightPrice,
                NightsCount = b.NightsCount,
                HotelTotal = b.HotelTotal,
                SeatsCount = b.TransportationSeats.Any() ? b.TransportationSeats.Count : b.SeatsCount,
                SeatPrice = b.TransportationSeats.FirstOrDefault()?.SeatPrice ?? b.SeatPrice,
                TransportationTotal = b.TransportationTotal,
                Discount = b.Discount,
                PaidAmount = b.PaidAmount,
                GrandTotal = b.GrandTotal,
                RemainingAmount = b.RemainingAmount,
                Notes = b.Notes,
                CreatedBy = !string.IsNullOrWhiteSpace(b.User.FullName) ? b.User.FullName : b.User.UserName,
                CreatedAtUtc = b.CreatedAtUtc,
                UpdatedAtUtc = b.UpdatedAtUtc,
                PhoneNumbers = b.PhoneNumbers.OrderByDescending(p => p.Prime).Select(p => new BookingPhoneDTO { Id = p.Id, PhoneNumber = p.PhoneNumber, Prime = p.Prime }).ToList(),
                Rooms = b.Rooms.Select(r => new BookingRoomDTO { BookingRoomId = r.BookingRoomId, RoomType = r.RoomType, Count = r.Count, NightPrice = r.NightPrice }).ToList(),
                Payments = b.Payments.Select(p => new BookingPaymentDTO { PaymentId = p.PaymentId, Amount = p.Amount, PayType = p.PayType, PaidAt = p.PaidAtUtc.ToLocalTime(), Notes = p.Notes }).ToList(),
                AuditLogs = b.AuditLogs.OrderByDescending(a => a.ChangedAtUtc).Select(a => new BookingAuditDTO
                {
                    AuditId = a.AuditId,
                    ActionType = a.ActionType,
                    ChangedAtUtc = a.ChangedAtUtc,
                    ChangedBy = !string.IsNullOrWhiteSpace(a.ChangedByUser.FullName) ? a.ChangedByUser.FullName : a.ChangedByUser.UserName,
                    Notes = a.Notes,
                    Details = a.Details.Select(d => new BookingAuditDetailDTO { FieldName = d.FieldName, OldValue = d.OldValue, NewValue = d.NewValue }).ToList()
                }).ToList()
            };
        }

        private static List<string> ValidateAllowedEditDto(UpdateBookingDTO dto, BookingData booking, bool isAdmin)
        {
            var errors = new List<string>();

            if (booking.HasHotel)
            {
                if (string.IsNullOrWhiteSpace(dto.HotelName))
                    errors.Add("Hotel name is required.");

                if (dto.CheckInDate == default)
                    errors.Add("Check in date is required.");

                if (dto.CheckOutDate == default)
                    errors.Add("Check out date is required.");

                if (dto.CheckOutDate.Date <= dto.CheckInDate.Date)
                    errors.Add("Check-out date must be after check-in date.");

                if (dto.NightsCount < 1)
                    errors.Add("Nights count must be at least 1.");

                var actualNights = Math.Max(1, (dto.CheckOutDate.Date - dto.CheckInDate.Date).Days);
                if (dto.NightsCount != actualNights)
                    errors.Add("Nights count must match check-in and check-out dates.");

                if (dto.Rooms == null || !dto.Rooms.Any(x => x.Count > 0))
                    errors.Add("At least one room is required.");

                foreach (var room in dto.Rooms ?? new List<BookingRoomDTO>())
                {
                    if (room.Count < 1)
                        errors.Add("Room count must be at least 1.");

                    if (room.NightPrice < 0)
                        errors.Add("Room night price cannot be negative.");
                }

                if (dto.ChildrenCountUntil6Years < 0 || dto.ChildrenCountUntil12Years < 0)
                    errors.Add("Children counts cannot be negative.");

                if (dto.TotalChildrenCount != dto.ChildrenCountUntil6Years + dto.ChildrenCountUntil12Years)
                    errors.Add("Total children count must equal children under 6 plus children under 12.");
            }

            if (isAdmin && dto.Discount < 0)
                errors.Add("Discount cannot be negative.");

            if (dto.PhoneNumbers == null || dto.PhoneNumbers.Count == 0 || dto.PhoneNumbers.All(x => string.IsNullOrWhiteSpace(x.PhoneNumber)))
                errors.Add("At least one phone number is required.");

            foreach (var phone in dto.PhoneNumbers ?? new List<BookingPhoneDTO>())
            {
                if (string.IsNullOrWhiteSpace(phone.PhoneNumber))
                    continue;

                if (!System.Text.RegularExpressions.Regex.IsMatch(phone.PhoneNumber.Trim(), @"^01[0-2,5][0-9]{8}$"))
                    errors.Add($"Invalid phone number: {phone.PhoneNumber}");
            }

            if (booking.HasTransportation)
            {
                if (dto.Transportation == null)
                {
                    errors.Add("Transportation data is required.");
                }
                else
                {
                    var requestedTripType = dto.Transportation.TripType;

                    var hasGo = booking.TransportationSeats.Any(x => x.Direction == TripDirection.Departure);
                    var hasReturn = booking.TransportationSeats.Any(x => x.Direction == TripDirection.Return);

                    if ((requestedTripType == TransportationTripType.Departure || requestedTripType == TransportationTripType.RoundTrip) && !hasGo)
                        errors.Add("Cannot enable Go trip because this booking has no existing Go seats.");

                    if ((requestedTripType == TransportationTripType.Return || requestedTripType == TransportationTripType.RoundTrip) && !hasReturn)
                        errors.Add("Cannot enable Return trip because this booking has no existing Return seats.");

                    if ((requestedTripType == TransportationTripType.Departure || requestedTripType == TransportationTripType.RoundTrip) &&
                        (!dto.Transportation.DepartureDate.HasValue || dto.Transportation.DepartureDate.Value == default))
                        errors.Add("Go travel date is required.");

                    if ((requestedTripType == TransportationTripType.Return || requestedTripType == TransportationTripType.RoundTrip) &&
                        (!dto.Transportation.ReturnDate.HasValue || dto.Transportation.ReturnDate.Value == default))
                        errors.Add("Return travel date is required.");

                    if (!Enum.IsDefined(typeof(TransportationTripType), requestedTripType))
                        errors.Add("Invalid trip type.");
                }
            }

            return errors.Distinct().ToList();
        }

        private async Task ApplyAllowedTransportationEditAsync(BookingData booking, TransportationBookingDTO transportation, BookingAudit audit)
        {
            var requestedTripType = transportation.TripType;

            var keepGo = requestedTripType == TransportationTripType.Departure ||
                         requestedTripType == TransportationTripType.RoundTrip;

            var keepReturn = requestedTripType == TransportationTripType.Return ||
                             requestedTripType == TransportationTripType.RoundTrip;

            var goSeats = booking.TransportationSeats
                .Where(x => x.Direction == TripDirection.Departure)
                .ToList();

            var returnSeats = booking.TransportationSeats
                .Where(x => x.Direction == TripDirection.Return)
                .ToList();

            if (!keepGo && goSeats.Any())
            {
                AddChange(audit, "GoSeatsRemoved", goSeats.Count, 0);
                _db.BookingTransportationSeats.RemoveRange(goSeats);
            }

            if (!keepReturn && returnSeats.Any())
            {
                AddChange(audit, "ReturnSeatsRemoved", returnSeats.Count, 0);
                _db.BookingTransportationSeats.RemoveRange(returnSeats);
            }

            if (keepGo && transportation.DepartureDate.HasValue)
            {
                await UpdateTripDateAsync(
                    goSeats,
                    transportation.DepartureDate.Value.Date,
                    "GoTravelDate",
                    audit);
            }

            if (keepReturn && transportation.ReturnDate.HasValue)
            {
                await UpdateTripDateAsync(
                    returnSeats,
                    transportation.ReturnDate.Value.Date,
                    "ReturnTravelDate",
                    audit);
            }
        }

        private async Task UpdateTripDateAsync(List<BookingTransportationSeat> seats, DateTime newDate, string auditFieldName, BookingAudit audit)
        {
            var trips = seats
                .Where(x => x.Trip != null)
                .Select(x => x.Trip)
                .DistinctBy(x => x.TripId)
                .ToList();

            foreach (var trip in trips)
            {
                var oldDate = trip.TripDate.Date;
                AddChange(audit, auditFieldName, oldDate, newDate);

                if (oldDate == newDate)
                    continue;

                var sameBusTrip = await _db.BusTrips
                    .FirstOrDefaultAsync(x =>
                        x.TripId != trip.TripId &&
                        x.BusId == trip.BusId &&
                        x.Direction == trip.Direction &&
                        x.TripDate.Date == newDate.Date &&
                        !x.IsClosed);

                if (sameBusTrip != null)
                {
                    foreach (var seat in seats.Where(x => x.TripId == trip.TripId))
                        seat.TripId = sameBusTrip.TripId;

                    var stillUsed = await _db.BookingTransportationSeats.AnyAsync(x => x.TripId == trip.TripId);
                    if (!stillUsed)
                        _db.BusTrips.Remove(trip);
                }
                else
                {
                    trip.TripDate = newDate.Date;
                }
            }
        }

        private static TransportationTripType GetCurrentTripType(BookingData booking)
        {
            var hasGo = booking.TransportationSeats.Any(x => x.Direction == TripDirection.Departure);
            var hasReturn = booking.TransportationSeats.Any(x => x.Direction == TripDirection.Return);

            if (hasGo && hasReturn)
                return TransportationTripType.RoundTrip;

            if (hasReturn)
                return TransportationTripType.Return;

            return TransportationTripType.Departure;
        }

        private static IEnumerable<BookingRoomDTO> NormalizeRooms(IEnumerable<BookingRoomDTO>? rooms)
        {
            return (rooms ?? Enumerable.Empty<BookingRoomDTO>())
                .Where(x => x.Count > 0)
                .Select(x => new BookingRoomDTO
                {
                    BookingRoomId = x.BookingRoomId,
                    RoomType = x.RoomType,
                    Count = x.Count,
                    NightPrice = x.NightPrice
                })
                .ToList();
        }

        private static string FormatRooms(IEnumerable<BookingRoom> rooms)
        {
            return string.Join(", ",
                rooms
                    .OrderBy(x => x.RoomType)
                    .ThenBy(x => x.NightPrice)
                    .Select(x => $"{x.Count} {x.RoomType} x {x.NightPrice:0.##}"));
        }

        private static string FormatRooms(IEnumerable<BookingRoomDTO> rooms)
        {
            return string.Join(", ",
                rooms
                    .OrderBy(x => x.RoomType)
                    .ThenBy(x => x.NightPrice)
                    .Select(x => $"{x.Count} {x.RoomType} x {x.NightPrice:0.##}"));
        }

        private static string FormatPhones(IEnumerable<Telephones> phones)
        {
            return string.Join(", ",
                phones
                    .OrderByDescending(x => x.Prime)
                    .ThenBy(x => x.PhoneNumber)
                    .Select(x => $"{x.PhoneNumber}{(x.Prime ? " (Primary)" : "")}"));
        }

        private static string FormatPhones(IEnumerable<BookingPhoneDTO> phones)
        {
            return string.Join(", ",
                phones
                    .OrderByDescending(x => x.Prime)
                    .ThenBy(x => x.PhoneNumber)
                    .Select(x => $"{x.PhoneNumber}{(x.Prime ? " (Primary)" : "")}"));
        }

        private static List<TripSeatSnapshotDTO> GetSeatsFromSnapshot(BusTrip trip)
        {
            if (!string.IsNullOrWhiteSpace(trip.SeatsSnapshotJson))
            {
                try
                {
                    var seats = System.Text.Json.JsonSerializer.Deserialize<List<TripSeatSnapshotDTO>>(
                        trip.SeatsSnapshotJson,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                    if (seats != null && seats.Count > 0)
                        return seats;
                }
                catch
                {
                    // fallback below
                }
            }

            return trip.Bus?.Seats?.Select(x => new TripSeatSnapshotDTO
            {
                SeatId = x.SeatId,
                SeatNumber = x.SeatNumber,
                SeatLabel = x.SeatLabel,
                SeatType = x.SeatType,
                RowNumber = x.RowNumber,
                ColumnNumber = x.ColumnNumber,
                IsActive = x.IsActive
            }).ToList() ?? new List<TripSeatSnapshotDTO>();
        }

        private static string GetSnapshotSeatLabel(Dictionary<Guid, TripSeatSnapshotDTO> seatsMap, Guid seatId)
        {
            if (!seatsMap.TryGetValue(seatId, out var seat))
                return "?";

            return string.IsNullOrWhiteSpace(seat.SeatLabel)
                ? seat.SeatNumber.ToString()
                : seat.SeatLabel;
        }
    }
}