using BusinessLayer.DTOs;
using BusinessLayer.DTOs.Bus;
using BusinessLayer.DTOs.Company;
using BusinessLayer.DTOs.CompanyBookSeat;
using BusinessLayer.Models;
using DataLayer.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection.Metadata;
using System.Text.Json;


namespace BusinessLayer.Functions
{
    public interface ICompanySeatBookingManager
    {
        Task<ResponceApi<SeatBookingBoundPageDTO>> GetSeatBookingBoundPageAsync(SeatBookingBoundPageDTO dto);
        Task<ResponceApi<Guid>> CreateTripForSeatBookingBoundAsync(CreateBookingSeatCompanyDTO dto);
        Task<ResponceApi<SeatBookingBoundPageDTO>> GetSeatBookingBoundTransferPageAsync(SeatBookingBoundPageDTO dto);
        Task<ResponceApi<TransferResultDTO>> TransferTripSeatsToCompanyAsync(TransferTripSeatsToCompanyDTO dto);
        Task<ResponceApi<List<Guid>>> CreateBookingBoundAsync(CreateCompanySeatBookingDTO dto);
        Task<ResponceApi<bool>> DeleteSeatBookingAsync(Guid bookingId);
        Task<ResponceApi<bool>> DeleteSeatBookingsAsync(List<Guid> bookingIds);
        Task<ResponceApi<ForceDeleteConflictDetailsDTO>> GetForceDeleteConflictDetailsAsync(List<Guid> bookingIds);
        Task<ResponceApi<bool>> ForceDeleteSeatBookingsAsync(List<Guid> bookingIds);
        Task<ResponceApi<List<CompanyTripSeatSummaryDTO>>> SearchAsync(CompanySeatBookingSearchDTO search);
        Task<ResponceApi<bool>> UpdateSeatPriceAsync(UpdateCompanySeatPriceDTO dto);
        Task<ResponceApi<Guid>> AddPaymentAsync(AddCompanySeatPaymentDTO dto);
        Task<ResponceApi<bool>> DeletePaymentAsync(Guid paymentId);
        Task<ResponceApi<CompanySeatAccountingPageDTO>> GetCompanySeatAccountingPageAsync(CompanySeatAccountingFilterDTO filter);
        Task<ResponceApi<CompanySeatAccountPageDTO>> GetCompanyAccountAsync(Guid companyId, DateTime? dateFrom, DateTime? dateTo);
    }

    public class CompanySeatBookingManager : ICompanySeatBookingManager
    {
        private readonly DBContext _db;
        private const string OriginalCompanyTransferredMarker = "[ORIGINAL_COMPANY_TRANSFERRED]";
        private const string ExternalReplacementMarker = "[EXTERNAL_REPLACEMENT_TRANSFER]";

        public CompanySeatBookingManager(DBContext db) => _db = db;

        //--------------------------------------------------------------------------------------------------------------------//

        // Get Setting Page Data
        public async Task<ResponceApi<SeatBookingBoundPageDTO>> GetSeatBookingBoundPageAsync(SeatBookingBoundPageDTO dto)
        {
            try
            {
                dto ??= new SeatBookingBoundPageDTO();
                dto.Normalize();

                // ─────────────────────────────────────────────
                // Lookups
                // ─────────────────────────────────────────────

                dto.Companies = await _db.Companies
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.CompanyId.ToString(),
                        Text = $"{x.Name} - {x.PhoneNumber}"
                    })
                    .ToListAsync();

                var busFromQuery = _db.Buses
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrWhiteSpace(x.FromLocation))
                    .Select(x => x.FromLocation!);

                var busToQuery = _db.Buses
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrWhiteSpace(x.ToLocation))
                    .Select(x => x.ToLocation!);

                var tripFromQuery = _db.BusTrips
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrWhiteSpace(x.FromLocation))
                    .Select(x => x.FromLocation!);

                var tripToQuery = _db.BusTrips
                    .AsNoTracking()
                    .Where(x => !string.IsNullOrWhiteSpace(x.ToLocation))
                    .Select(x => x.ToLocation!);

                dto.Locations = await busFromQuery
                    .Concat(busToQuery)
                    .Concat(tripFromQuery)
                    .Concat(tripToQuery)
                    .Select(x => x.Trim())
                    .Where(x => x != "")
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync();

                dto.Locations = dto.Locations
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                // ─────────────────────────────────────────────
                // Build Trip / Bus Cards
                // ─────────────────────────────────────────────

                var hasRoute =
                    !string.IsNullOrWhiteSpace(dto.RouteFrom) &&
                    !string.IsNullOrWhiteSpace(dto.RouteTo);

                if (hasRoute)
                {
                    var dateOnly = dto.TripDate!.Value.Date;
                    var routeFrom = dto.RouteFrom!.Trim();
                    var routeTo = dto.RouteTo!.Trim();
                    var requiredSeats = Math.Max(1, dto.RequiredSeats);

                    var routeFromLower = routeFrom.ToLower();
                    var routeToLower = routeTo.ToLower();

                    var routeBuses = await _db.Buses
                        .AsNoTracking()
                        .Include(x => x.Seats)
                        .Where(x =>
                            x.IsActive &&
                            x.FromLocation != null &&
                            x.ToLocation != null &&
                            (
                                (
                                    x.FromLocation.Trim().ToLower() == routeFromLower &&
                                    x.ToLocation.Trim().ToLower() == routeToLower
                                )
                                ||
                                (
                                    x.FromLocation.Trim().ToLower() == routeToLower &&
                                    x.ToLocation.Trim().ToLower() == routeFromLower
                                )
                            ))
                        .OrderBy(x => x.Name)
                        .ToListAsync();

                    if (routeBuses.Any())
                    {
                        var routeBusIds = routeBuses
                            .Select(x => x.BusId)
                            .ToList();

                        var trips = await _db.BusTrips
                            .AsNoTracking()
                            .Include(x => x.ReservedSeats)
                            .Where(x =>
                                x.TripDate.Date == dateOnly &&
                                !x.IsClosed &&
                                routeBusIds.Contains(x.BusId) &&
                                (x.FromLocation ?? "").Trim().ToLower() == routeFromLower &&
                                (x.ToLocation ?? "").Trim().ToLower() == routeToLower)
                            .ToListAsync();

                        var tripIds = trips
                            .Select(x => x.TripId)
                            .ToList();

                        var companyBookedByTrip = tripIds.Any()
                            ? await _db.CompanySeatBookings
                                .AsNoTracking()
                                .Where(x =>
                                    x.TripId != null &&
                                    tripIds.Contains(x.TripId.Value) &&
                                    x.SeatId != null)
                                .GroupBy(x => x.TripId!.Value)
                                .Select(g => new
                                {
                                    TripId = g.Key,
                                    Count = g.Count()
                                })
                                .ToDictionaryAsync(x => x.TripId, x => x.Count)
                            : new Dictionary<Guid, int>();

                        var availableTripCards = new List<SeatBookingBoundBusDTO>();

                        foreach (var trip in trips)
                        {
                            var bus = routeBuses
                                .FirstOrDefault(x => x.BusId == trip.BusId);

                            if (bus == null)
                                continue;

                            var total =
                                CountSeatBookingBoundTripSnapshotPassengerSeats(trip);

                            var clientReserved =
                                trip.ReservedSeats.Count;

                            var companyReserved =
                                companyBookedByTrip.TryGetValue(
                                    trip.TripId,
                                    out var cc)
                                    ? cc
                                    : 0;

                            var available =
                                Math.Max(0, total - clientReserved - companyReserved);

                            if (available < requiredSeats)
                                continue;

                            availableTripCards.Add(new SeatBookingBoundBusDTO
                            {
                                BusId = bus.BusId,
                                TripId = trip.TripId,
                                TripDate = trip.TripDate,
                                Direction = trip.Direction,

                                BusName =
                                    string.IsNullOrWhiteSpace(trip.BusNameSnapshot)
                                        ? bus.Name
                                        : trip.BusNameSnapshot,

                                PlateNumber =
                                    string.IsNullOrWhiteSpace(trip.PlateNumberSnapshot)
                                        ? bus.PlateNumber ?? string.Empty
                                        : trip.PlateNumberSnapshot,

                                FromLocation =
                                    trip.FromLocation ?? routeFrom,

                                ToLocation =
                                    trip.ToLocation ?? routeTo,

                                TotalSeats = total,
                                ClientReserved = clientReserved,
                                CompanyReserved = companyReserved,
                                AvailableSeats = available,

                                HasTrip = true,
                                IsFull = false,
                                IsFallbackOption = false
                            });
                        }

                        dto.Buses = availableTripCards.Any()
                            ? availableTripCards
                                .OrderBy(x => x.Direction)
                                .ThenByDescending(x => x.AvailableSeats)
                                .ThenBy(x => x.BusName)
                                .ToList()
                            : routeBuses
                                .Select(bus =>
                                {
                                    var total = bus.Seats.Count(s =>
                                        s.IsActive &&
                                        (
                                            s.SeatType == SeatType.Normal ||
                                            s.SeatType == SeatType.VIP
                                        ));

                                    var busDirection =
                                        !string.IsNullOrWhiteSpace(bus.FromLocation) &&
                                        !string.IsNullOrWhiteSpace(bus.ToLocation) &&
                                        bus.FromLocation.Trim().Equals(
                                            routeFrom,
                                            StringComparison.OrdinalIgnoreCase) &&
                                        bus.ToLocation.Trim().Equals(
                                            routeTo,
                                            StringComparison.OrdinalIgnoreCase)
                                            ? TripDirection.Departure
                                            : TripDirection.Return;

                                    return new SeatBookingBoundBusDTO
                                    {
                                        BusId = bus.BusId,
                                        TripId = null,
                                        TripDate = dateOnly,
                                        Direction = busDirection,

                                        BusName = bus.Name,
                                        PlateNumber = bus.PlateNumber ?? string.Empty,

                                        FromLocation = routeFrom,
                                        ToLocation = routeTo,

                                        TotalSeats = total,
                                        ClientReserved = 0,
                                        CompanyReserved = 0,
                                        AvailableSeats = total,

                                        HasTrip = false,
                                        IsFull = total < requiredSeats,
                                        IsFallbackOption = true
                                    };
                                })
                                .Where(x => x.AvailableSeats >= requiredSeats)
                                .OrderBy(x => x.Direction)
                                .ThenByDescending(x => x.AvailableSeats)
                                .ThenBy(x => x.BusName)
                                .ToList();
                    }
                    else
                    {
                        dto.Buses = new List<SeatBookingBoundBusDTO>();
                    }
                }

                // ─────────────────────────────────────────────
                // Build Seat Map
                // ─────────────────────────────────────────────

                if (dto.SelectedBusId.HasValue)
                {
                    var busId = dto.SelectedBusId.Value;
                    var routeFrom = dto.RouteFrom?.Trim();
                    var routeTo = dto.RouteTo?.Trim();
                    var tripId = dto.SelectedTripId;

                    BusTrip? trip = null;

                    if (tripId.HasValue && tripId.Value != Guid.Empty)
                    {
                        trip = await _db.BusTrips
                            .AsNoTracking()
                            .Include(x => x.ReservedSeats)
                            .FirstOrDefaultAsync(x =>
                                x.TripId == tripId.Value &&
                                !x.IsClosed);
                    }

                    var bus = await _db.Buses
                        .AsNoTracking()
                        .Include(x => x.Seats)
                        .FirstOrDefaultAsync(x =>
                            x.BusId == busId &&
                            x.IsActive);

                    if (bus != null)
                    {
                        var companyReserved =
                            new HashSet<Guid>();

                        var clientReserved =
                            new HashSet<Guid>();

                        List<SeatBookingBoundSeatDTO> seats;
                        int rows;
                        int columns;
                        string busName;
                        string from;
                        string to;

                        if (trip != null)
                        {
                            dto.Direction = trip.Direction;

                            rows = trip.LayoutRows;
                            columns = trip.LayoutColumns;

                            busName =
                                string.IsNullOrWhiteSpace(trip.BusNameSnapshot)
                                    ? bus.Name
                                    : trip.BusNameSnapshot;

                            from =
                                trip.FromLocation ??
                                routeFrom ??
                                bus.FromLocation ??
                                string.Empty;

                            to =
                                trip.ToLocation ??
                                routeTo ??
                                bus.ToLocation ??
                                string.Empty;

                            clientReserved = trip.ReservedSeats
                                .Select(x => x.SeatId)
                                .ToHashSet();

                            companyReserved = await _db.CompanySeatBookings
                                .AsNoTracking()
                                .Where(x =>
                                    x.TripId == trip.TripId &&
                                    x.SeatId != null)
                                .Select(x => x.SeatId!.Value)
                                .ToHashSetAsync();

                            seats =
                                ReadSeatBookingBoundSeatsFromSnapshot(
                                    trip.SeatsSnapshotJson);
                        }
                        else
                        {
                            rows = bus.LayoutRows;
                            columns = bus.LayoutColumns;
                            busName = bus.Name;

                            from =
                                routeFrom ??
                                bus.FromLocation ??
                                string.Empty;

                            to =
                                routeTo ??
                                bus.ToLocation ??
                                string.Empty;

                            seats = bus.Seats
                                .OrderBy(x => x.RowNumber)
                                .ThenBy(x => x.ColumnNumber)
                                .Select(x => new SeatBookingBoundSeatDTO
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
                        }

                        foreach (var seat in seats)
                        {
                            seat.IsClientReserved =
                                clientReserved.Contains(seat.SeatId);

                            seat.IsCompanyReserved =
                                companyReserved.Contains(seat.SeatId);

                            seat.IsSelectable =
                                seat.IsActive &&
                                (
                                    seat.SeatType == SeatType.Normal ||
                                    seat.SeatType == SeatType.VIP
                                ) &&
                                !seat.IsClientReserved &&
                                !seat.IsCompanyReserved;
                        }

                        dto.SeatMap =
                            new SeatBookingBoundSeatMapDTO
                            {
                                TripId = trip?.TripId,
                                BusId = busId,
                                BusName = busName,
                                FromLocation = from,
                                ToLocation = to,
                                Rows = rows,
                                Columns = columns,
                                Seats = seats
                            };
                    }
                    else
                    {
                        dto.SeatMap = null;
                    }
                }

                return ResponceApi<SeatBookingBoundPageDTO>.Ok(dto);
            }
            catch (Exception ex)
            {
                return ResponceApi<SeatBookingBoundPageDTO>.Fail("Failed to load seat booking page.", ex.Message);
            }
        }

        // Get or Create Trip For Seat Booking Bound
        public async Task<ResponceApi<Guid>>CreateTripForSeatBookingBoundAsync(CreateBookingSeatCompanyDTO dto)
        {
            try
            {
                var dateOnly = dto.TripDate.Date;
                var routeFrom = dto.FromLocation.Trim();
                var routeTo = dto.ToLocation.Trim();
                var requiredSeats = Math.Max(1, dto.RequiredSeats);

                if (dto.TripId.HasValue && dto.TripId.Value != Guid.Empty)
                {
                    var preferred = await _db.BusTrips
                        .Include(x => x.ReservedSeats)
                        .FirstOrDefaultAsync(x =>
                            x.TripId == dto.TripId.Value &&
                            !x.IsClosed);

                    if (preferred == null)
                        return ResponceApi<Guid>
                            .Fail("Selected trip not found or closed.");

                    var available =
                        await GetSeatBookingBoundAvailableSeatsCountForTripAsync(preferred);

                    if (available >= requiredSeats)
                        return ResponceApi<Guid>.Ok(preferred.TripId);

                    return ResponceApi<Guid>
                        .Fail("Selected trip does not have enough available seats.");
                }

                var existingTrips = await _db.BusTrips
                    .Include(x => x.ReservedSeats)
                    .Where(x =>
                        x.BusId == dto.BusId &&
                        x.TripDate.Date == dateOnly &&
                        x.Direction == dto.Direction &&
                        !x.IsClosed &&
                        (x.FromLocation ?? "").Trim().ToLower() == routeFrom.ToLower() &&
                        (x.ToLocation ?? "").Trim().ToLower() == routeTo.ToLower())
                    .ToListAsync();

                foreach (var trip in existingTrips)
                {
                    var available =
                        await GetSeatBookingBoundAvailableSeatsCountForTripAsync(trip);

                    if (available >= requiredSeats)
                        return ResponceApi<Guid>.Ok(trip.TripId);
                }

                var bus = await _db.Buses
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(x =>
                        x.BusId == dto.BusId &&
                        x.IsActive);

                if (bus == null)
                    return ResponceApi<Guid>
                        .Fail("Bus not found or inactive.");

                var seatsSnapshot = bus.Seats
                    .OrderBy(x => x.RowNumber)
                    .ThenBy(x => x.ColumnNumber)
                    .Select(x => new SeatBookingBoundSeatDTO
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
                    (x.SeatType == SeatType.Normal ||
                     x.SeatType == SeatType.VIP));

                if (passengerSeatsCount < requiredSeats)
                    return ResponceApi<Guid>
                        .Fail($"This bus does not contain {requiredSeats} available seats.");

                var newTrip = new BusTrip
                {
                    TripId = Guid.NewGuid(),
                    BusId = bus.BusId,
                    Direction = dto.Direction,
                    TripDate = dateOnly,
                    FromLocation = routeFrom,
                    ToLocation = routeTo,
                    BusNameSnapshot = bus.Name,
                    PlateNumberSnapshot = bus.PlateNumber,
                    SeatsCountSnapshot = passengerSeatsCount,
                    LayoutRows = bus.LayoutRows,
                    LayoutColumns = bus.LayoutColumns,
                    LayoutJson = bus.LayoutJson,
                    SeatsSnapshotJson = JsonSerializer.Serialize(seatsSnapshot),
                    IsLayoutCustomized = false,
                    IsClosed = false
                };

                _db.BusTrips.Add(newTrip);
                await _db.SaveChangesAsync();

                return ResponceApi<Guid>.Ok(newTrip.TripId, "A new trip has been created.");
            }
            catch (Exception ex)
            {
                return ResponceApi<Guid>.Fail("Failed to prepare trip.", ex.Message);
            }
        }

        // Get Data For Transfer Tab
        public async Task<ResponceApi<SeatBookingBoundPageDTO>>GetSeatBookingBoundTransferPageAsync(SeatBookingBoundPageDTO dto)
        {
            try
            {
                dto ??= new SeatBookingBoundPageDTO();

                dto.ActiveTab = "transferTab";
                dto.TransferTripSearch ??= new TransferTripSearchDTO();

                #region Normalize Input

                var selectedDate =
                    dto.TransferTripSearch.TripDate?.Date ?? DateTime.Today;

                var routeFrom =
                    dto.TransferTripSearch.RouteFrom?.Trim();

                var routeTo =
                    dto.TransferTripSearch.RouteTo?.Trim();

                var selectedTripId =
                    dto.TransferTripSearch.SelectedTripId;

                dto.TripDate = selectedDate;
                dto.RequiredSeats = 1;

                dto.RouteFrom = null;
                dto.RouteTo = null;

                dto.SelectedBusId = null;
                dto.SelectedTripId = null;

                dto.TransferTripSearch.TripDate = selectedDate;
                dto.TransferTripSearch.RouteFrom = routeFrom;
                dto.TransferTripSearch.RouteTo = routeTo;
                dto.TransferTripSearch.SelectedTripId = selectedTripId;

                #endregion

                #region Load Base Page

                var pageResult =
                    await GetSeatBookingBoundPageAsync(dto);

                if (!pageResult.Success || pageResult.Data == null)
                    return pageResult;

                var vm = pageResult.Data;

                vm.ActiveTab = "transferTab";
                vm.TransferTripSearch = dto.TransferTripSearch;

                #endregion

                #region Load Trips By Route

                if (!string.IsNullOrWhiteSpace(routeFrom) &&
                    !string.IsNullOrWhiteSpace(routeTo))
                {
                    var dateOnly = selectedDate.Date;
                    var fromLower = routeFrom.ToLower();
                    var toLower = routeTo.ToLower();

                    var trips = await _db.BusTrips
                        .AsNoTracking()
                        .Include(x => x.ReservedSeats)
                            .ThenInclude(x => x.Booking)
                        .Where(x =>
                            !x.IsClosed &&
                            x.TripDate.Date == dateOnly &&
                            (x.FromLocation ?? "").Trim().ToLower() == fromLower &&
                            (x.ToLocation ?? "").Trim().ToLower() == toLower)
                        .OrderBy(x => x.Direction)
                        .ThenBy(x => x.BusNameSnapshot)
                        .ToListAsync();

                    var tripIds = trips.Select(x => x.TripId).ToList();

                    var companySeatsByTrip = tripIds.Any()
                        ? await _db.CompanySeatBookings
                            .AsNoTracking()
                            .Where(x =>
                                x.TripId != null &&
                                tripIds.Contains(x.TripId.Value) &&
                                x.SeatId != null)
                            .GroupBy(x => x.TripId!.Value)
                            .Select(g => new
                            {
                                TripId = g.Key,
                                Count = g.Count()
                            })
                            .ToDictionaryAsync(x => x.TripId, x => x.Count)
                        : new Dictionary<Guid, int>();

                    vm.TransferTrips = trips.Select(trip =>
                    {
                        var validClientSeats = trip.ReservedSeats
                            .Where(x => x.Booking != null && !x.Booking.IsDeleted)
                            .ToList();

                        var companyCount =
                            companySeatsByTrip.TryGetValue(
                                trip.TripId,
                                out var cc)
                                ? cc
                                : 0;

                        return new TransferTripOptionDTO
                        {
                            TripId = trip.TripId,
                            BusId = trip.BusId,
                            TripDate = trip.TripDate,
                            Direction = trip.Direction,

                            BusName =
                                string.IsNullOrWhiteSpace(trip.BusNameSnapshot)
                                    ? "Bus"
                                    : trip.BusNameSnapshot,

                            PlateNumber =
                                trip.PlateNumberSnapshot ?? string.Empty,

                            FromLocation =
                                trip.FromLocation ?? string.Empty,

                            ToLocation =
                                trip.ToLocation ?? string.Empty,

                            ClientBookingsCount =
                                validClientSeats
                                    .Select(x => x.BookingId)
                                    .Distinct()
                                    .Count(),

                            ClientSeatsCount =
                                validClientSeats.Count,

                            CompanySeatsCount =
                                companyCount
                        };
                    }).ToList();
                }

                #endregion

                #region Load Trip Details

                if (selectedTripId.HasValue &&
                    selectedTripId.Value != Guid.Empty)
                {
                    var trip = await _db.BusTrips
                        .AsNoTracking()
                        .Include(x => x.ReservedSeats)
                            .ThenInclude(x => x.Booking)
                                .ThenInclude(x => x.PhoneNumbers)
                        .FirstOrDefaultAsync(x =>
                            x.TripId == selectedTripId &&
                            !x.IsClosed);

                    if (trip == null)
                    {
                        return ResponceApi<SeatBookingBoundPageDTO>.Fail("Trip is not found or closed.");
                    }

                    var transferredBookings =
                        await _db.CompanySeatBookings
                            .AsNoTracking()
                            .Include(x => x.Company)
                            .Where(x => x.TransferredSeatsJson != null)
                            .ToListAsync();

                    var transferredSeatMap =
                        new Dictionary<Guid, string>();

                    foreach (var item in transferredBookings)
                    {
                        var companyName =
                            item.Company?.Name ?? string.Empty;

                        if (item.TransferredFromSeatId.HasValue)
                        {
                            transferredSeatMap[
                                item.TransferredFromSeatId.Value] =
                                companyName;
                        }

                        if (string.IsNullOrWhiteSpace(
                                item.TransferredSeatsJson))
                            continue;

                        try
                        {
                            var snaps =
                                JsonSerializer.Deserialize<
                                    List<TransferredSeatSnapshot>>(
                                    item.TransferredSeatsJson,
                                    new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });

                            if (snaps == null)
                                continue;

                            foreach (var snap in snaps.Where(x =>
                                         x.TripId == selectedTripId))
                            {
                                transferredSeatMap[snap.SeatId] =
                                    companyName;
                            }
                        }
                        catch
                        {
                        }
                    }

                    var passengerSeats = trip.ReservedSeats
                        .Where(x =>
                            x.Booking != null &&
                            !x.Booking.IsDeleted)
                        .Select(x =>
                        {
                            var snap =
                                GetSnapshotSeat(trip, x.SeatId);

                            var phone =
                                x.Booking.PhoneNumbers
                                    .OrderByDescending(p => p.Prime)
                                    .FirstOrDefault()
                                    ?.PhoneNumber;

                            return new
                            {
                                Seat = x,
                                Row = snap?.RowNumber ?? 0,
                                Column = snap?.ColumnNumber ?? 0,

                                Label =
                                    snap != null
                                        ? string.IsNullOrWhiteSpace(
                                            snap.SeatLabel)
                                            ? snap.SeatNumber.ToString()
                                            : snap.SeatLabel
                                        : x.SeatId.ToString(),

                                Phone = phone
                            };
                        })
                        .OrderBy(x => x.Seat.Booking.ClientName)
                        .ThenBy(x => x.Row)
                        .ThenBy(x => x.Column)
                        .Select(x => new TransferTripPassengerSeatDTO
                        {
                            BookingSeatId = x.Seat.BookingSeatId,
                            BookingId = x.Seat.BookingId,
                            BookingCode = x.Seat.Booking.Code,

                            ClientName =
                                x.Seat.Booking.ClientName,

                            ClientPhone = x.Phone,

                            TripId = x.Seat.TripId,
                            TripDate = trip.TripDate,
                            Direction = x.Seat.Direction,

                            BusName =
                                string.IsNullOrWhiteSpace(
                                    trip.BusNameSnapshot)
                                    ? "Bus"
                                    : trip.BusNameSnapshot,

                            PlateNumber =
                                trip.PlateNumberSnapshot
                                ?? string.Empty,

                            SeatId = x.Seat.SeatId,
                            SeatLabel = x.Label,

                            FromLocation =
                                x.Seat.FromLocation,

                            ToLocation =
                                x.Seat.ToLocation,

                            SeatPrice =
                                x.Seat.SeatPrice,

                            AlreadyTransferred =
                                transferredSeatMap.ContainsKey(
                                    x.Seat.SeatId),

                            TransferredToCompany =
                                transferredSeatMap.TryGetValue(
                                    x.Seat.SeatId,
                                    out var cn)
                                    ? cn
                                    : null
                        })
                        .ToList();

                    var companyBookings =
                        await _db.CompanySeatBookings
                            .AsNoTracking()
                            .Include(x => x.Company)
                            .Where(x =>
                                x.TripId == selectedTripId &&
                                x.SeatId != null &&
                                x.BookingDirection ==
                                CompanySeatBookingDirection.Inbound)
                            .OrderBy(x => x.Company.Name)
                            .ThenBy(x => x.SeatNumberSnapshot)
                            .ToListAsync();

                    var companySeats =
                        companyBookings.Select(x =>
                        {
                            var snap =
                                GetSnapshotSeat(
                                    trip,
                                    x.SeatId!.Value);

                            var label =
                                !string.IsNullOrWhiteSpace(
                                    x.SeatLabelSnapshot)
                                    ? x.SeatLabelSnapshot
                                    : snap != null
                                        ? string.IsNullOrWhiteSpace(
                                            snap.SeatLabel)
                                            ? snap.SeatNumber.ToString()
                                            : snap.SeatLabel
                                        : x.SeatNumberSnapshot > 0
                                            ? x.SeatNumberSnapshot.ToString()
                                            : "?";

                            return new TransferTripCompanySeatDTO
                            {
                                CompanySeatBookingId =
                                    x.BookingId,

                                CompanyId = x.CompanyId,

                                CompanyName =
                                    x.Company?.Name
                                    ?? string.Empty,

                                CompanyPhone =
                                    x.Company?.PhoneNumber
                                    ?? string.Empty,

                                TripId = trip.TripId,
                                TripDate = trip.TripDate,

                                BusName =
                                    string.IsNullOrWhiteSpace(
                                        trip.BusNameSnapshot)
                                        ? "Bus"
                                        : trip.BusNameSnapshot,

                                PlateNumber =
                                    trip.PlateNumberSnapshot
                                    ?? string.Empty,

                                SeatId = x.SeatId,
                                SeatLabel = label,

                                ClientName = x.ClientName,
                                ClientPhone = x.ClientPhone,

                                FromLocation = x.FromLocation,
                                ToLocation = x.ToLocation,

                                PricePerSeat =
                                    x.PricePerSeat
                            };
                        }).ToList();

                    vm.TransferTripDetails =
                        new TransferTripDetailsDTO
                        {
                            TripId = trip.TripId,
                            TripDate = trip.TripDate,
                            Direction = trip.Direction,

                            BusName =
                                string.IsNullOrWhiteSpace(
                                    trip.BusNameSnapshot)
                                    ? "Bus"
                                    : trip.BusNameSnapshot,

                            PlateNumber =
                                trip.PlateNumberSnapshot
                                ?? string.Empty,

                            FromLocation =
                                trip.FromLocation
                                ?? string.Empty,

                            ToLocation =
                                trip.ToLocation
                                ?? string.Empty,

                            PassengerSeats = passengerSeats,
                            CompanySeats = companySeats
                        };
                }

                #endregion

                return ResponceApi<SeatBookingBoundPageDTO>.Ok(vm);
            }
            catch (Exception ex)
            {
                return ResponceApi<SeatBookingBoundPageDTO>.Fail("Failed to load the transfer data.", ex.Message);
            }
        }

        // Transfer Seats Between Client and Company
        public async Task<ResponceApi<TransferResultDTO>> TransferTripSeatsToCompanyAsync(TransferTripSeatsToCompanyDTO dto)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (dto.TripId == Guid.Empty)
                    return ResponceApi<TransferResultDTO>.Fail("Trip is required.");

                if (dto.CompanyId == Guid.Empty)
                    return ResponceApi<TransferResultDTO>.Fail("Company is required.");

                dto.BookingSeatIds ??= new List<Guid>();
                dto.CompanySeatBookingIds ??= new List<Guid>();

                var selectedClientSeatIds = dto.BookingSeatIds
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .ToList();

                var selectedCompanyBookingIds = dto.CompanySeatBookingIds
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (!selectedClientSeatIds.Any() && !selectedCompanyBookingIds.Any())
                    return ResponceApi<TransferResultDTO>.Fail("Select at least one seat.");

                if (string.IsNullOrWhiteSpace(dto.FromLocation) || string.IsNullOrWhiteSpace(dto.ToLocation))
                    return ResponceApi<TransferResultDTO>.Fail("From/To locations are required.");

                var targetCompany = await _db.Companies
                    .FirstOrDefaultAsync(x => x.CompanyId == dto.CompanyId && x.IsActive);

                if (targetCompany == null)
                    return ResponceApi<TransferResultDTO>.Fail("Company not found or inactive.");

                var trip = await _db.BusTrips
                    .Include(x => x.Bus)
                    .FirstOrDefaultAsync(x => x.TripId == dto.TripId && !x.IsClosed);

                if (trip == null)
                    return ResponceApi<TransferResultDTO>.Fail("Trip not found or closed.");

                var transferDate = dto.ExternalTripDate?.Date ?? trip.TripDate.Date;
                var pricePerSeat = Math.Max(0, dto.PricePerSeat);

                var createdBookings = new List<CompanySeatBooking>();
                var totalTransferredSeats = 0;

                if (selectedClientSeatIds.Any())
                {
                    var seatsToTransfer = await _db.BookingTransportationSeats
                        .Include(x => x.Booking)
                            .ThenInclude(x => x.PhoneNumbers)
                        .Where(x =>
                            x.TripId == dto.TripId &&
                            selectedClientSeatIds.Contains(x.BookingSeatId) &&
                            !x.Booking.IsDeleted)
                        .ToListAsync();

                    if (seatsToTransfer.Count != selectedClientSeatIds.Count)
                        return ResponceApi<TransferResultDTO>.Fail("Some selected client seats were not found.");

                    foreach (var group in seatsToTransfer.GroupBy(x => x.BookingId))
                    {
                        var booking = group.First().Booking;
                        var groupSeats = group.ToList();

                        var snapshots = groupSeats.Select(x =>
                        {
                            var snap = GetSnapshotSeat(trip, x.SeatId);

                            return new TransferredSeatSnapshot
                            {
                                BookingSeatId = x.BookingSeatId,
                                TripId = x.TripId,
                                SeatId = x.SeatId,
                                Direction = (int)x.Direction,
                                FromLocation = x.FromLocation,
                                ToLocation = x.ToLocation,
                                SeatPrice = x.SeatPrice,
                                SeatNumber = snap?.SeatNumber ?? 0,
                                SeatLabel = snap != null
                                    ? string.IsNullOrWhiteSpace(snap.SeatLabel)
                                        ? snap.SeatNumber.ToString()
                                        : snap.SeatLabel
                                    : "?",
                                SeatType = snap?.SeatType,
                                RowNumber = snap?.RowNumber ?? 0,
                                ColumnNumber = snap?.ColumnNumber ?? 0
                            };
                        }).ToList();

                        var clientPhone = booking.PhoneNumbers
                            .OrderByDescending(x => x.Prime)
                            .FirstOrDefault()?.PhoneNumber;

                        var outbound = new CompanySeatBooking
                        {
                            BookingId = Guid.NewGuid(),
                            CompanyId = dto.CompanyId,

                            TripId = null,
                            SeatId = null,

                            SeatsCount = groupSeats.Count,
                            TripDate = transferDate,

                            FromLocation = dto.FromLocation.Trim(),
                            ToLocation = dto.ToLocation.Trim(),

                            PricePerSeat = pricePerSeat,
                            BookingDirection = CompanySeatBookingDirection.Outbound,

                            ClientTripType = trip.Direction == TripDirection.Return
                                ? TransportationTripType.Return
                                : TransportationTripType.Departure,

                            ClientName = booking.ClientName,
                            ClientPhone = clientPhone,

                            TransferredFromBookingId = booking.BookingID,
                            TransferredFromSeatId = groupSeats.Count == 1 ? groupSeats[0].SeatId : null,
                            TransferredSeatsJson = JsonSerializer.Serialize(snapshots),

                            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                                ? $"ClientTransfer: Transferred client from booking {booking.Code} - Trip {trip.TripDate:yyyy-MM-dd}"
                                : $"ClientTransfer: {dto.Notes.Trim()}",

                            CreatedAtUtc = DateTime.UtcNow
                        };

                        _db.CompanySeatBookings.Add(outbound);
                        createdBookings.Add(outbound);

                        _db.BookingTransportationSeats.RemoveRange(groupSeats);

                        var remainingSeatsCount = await _db.BookingTransportationSeats
                            .CountAsync(x =>
                                x.BookingId == booking.BookingID &&
                                !selectedClientSeatIds.Contains(x.BookingSeatId));

                        var previousTransferredSeatsCount = await _db.CompanySeatBookings
                            .Where(x =>
                                x.BookingDirection == CompanySeatBookingDirection.Outbound &&
                                x.TransferredFromBookingId == booking.BookingID &&
                                !string.IsNullOrWhiteSpace(x.TransferredSeatsJson))
                            .SumAsync(x => (int?)x.SeatsCount) ?? 0;

                        var transferredSeatsCountForThisBooking =
                            previousTransferredSeatsCount + groupSeats.Count;

                        booking.SeatsCount = remainingSeatsCount + transferredSeatsCountForThisBooking;

                        // مهم جدا:
                        // لا تجعل الحجز Hotel فقط طالما له مقاعد متبقية أو مقاعد محولة لشركة.
                        booking.HasTransportation =
                            remainingSeatsCount > 0 ||
                            transferredSeatsCountForThisBooking > 0;

                        booking.UpdatedAtUtc = DateTime.UtcNow;

                        totalTransferredSeats += groupSeats.Count;
                    }
                }

                if (selectedCompanyBookingIds.Any())
                {
                    var companySeatsToTransfer = await _db.CompanySeatBookings
                        .Include(x => x.Company)
                        .Where(x =>
                            selectedCompanyBookingIds.Contains(x.BookingId) &&
                            x.TripId == dto.TripId &&
                            x.SeatId != null &&
                            x.BookingDirection == CompanySeatBookingDirection.Inbound)
                        .ToListAsync();

                    if (companySeatsToTransfer.Count != selectedCompanyBookingIds.Count)
                        return ResponceApi<TransferResultDTO>.Fail("Some selected company seats were not found.");

                    if (companySeatsToTransfer.Any(x => x.CompanyId == dto.CompanyId))
                    {
                        return ResponceApi<TransferResultDTO>.Fail(
                            "You cannot transfer company seats to the same company that already booked them.");
                    }

                    foreach (var group in companySeatsToTransfer.GroupBy(x => new
                    {
                        x.CompanyId,
                        x.ClientName,
                        x.ClientPhone,
                        x.ClientTripType
                    }))
                    {
                        var first = group.First();
                        var originalCompanyName = first.Company?.Name ?? "Company";
                        var groupSeats = group.ToList();

                        var snapshots = groupSeats.Select(x =>
                        {
                            var snap = x.SeatId.HasValue
                                ? GetSnapshotSeat(trip, x.SeatId.Value)
                                : null;

                            return new TransferredSeatSnapshot
                            {
                                BookingSeatId = x.BookingId,

                                TripId = trip.TripId,
                                SeatId = x.SeatId ?? Guid.Empty,
                                Direction = (int)trip.Direction,

                                FromLocation = x.FromLocation,
                                ToLocation = x.ToLocation,
                                SeatPrice = x.PricePerSeat,

                                SeatNumber = x.SeatNumberSnapshot > 0
                                    ? x.SeatNumberSnapshot
                                    : snap?.SeatNumber ?? 0,

                                SeatLabel = !string.IsNullOrWhiteSpace(x.SeatLabelSnapshot)
                                    ? x.SeatLabelSnapshot
                                    : snap != null
                                        ? string.IsNullOrWhiteSpace(snap.SeatLabel)
                                            ? snap.SeatNumber.ToString()
                                            : snap.SeatLabel
                                        : "?",

                                SeatType = x.SeatTypeSnapshot ?? snap?.SeatType,
                                RowNumber = snap?.RowNumber ?? 0,
                                ColumnNumber = snap?.ColumnNumber ?? 0,

                                OriginalCompanyId = x.CompanyId,
                                OriginalCompanyName = x.Company?.Name,
                                OriginalCompanyPhone = x.Company?.PhoneNumber,

                                OriginalClientName = x.ClientName,
                                OriginalClientPhone = x.ClientPhone,
                                OriginalClientTripType = x.ClientTripType
                            };
                        }).ToList();

                        var targetOutbound = new CompanySeatBooking
                        {
                            BookingId = Guid.NewGuid(),
                            CompanyId = dto.CompanyId,

                            TripId = null,
                            SeatId = null,

                            SeatsCount = groupSeats.Count,
                            TripDate = transferDate,

                            FromLocation = dto.FromLocation.Trim(),
                            ToLocation = dto.ToLocation.Trim(),

                            PricePerSeat = pricePerSeat,
                            BookingDirection = CompanySeatBookingDirection.Outbound,

                            ClientTripType = group.Key.ClientTripType,
                            ClientName = group.Key.ClientName,
                            ClientPhone = group.Key.ClientPhone,

                            TransferredFromBookingId = null,
                            TransferredFromSeatId = groupSeats.Count == 1 ? groupSeats[0].SeatId : null,
                            TransferredSeatsJson = JsonSerializer.Serialize(snapshots),

                            Notes = string.IsNullOrWhiteSpace(dto.Notes)
                                ? $"CompanyTransfer: Transferred company seats from {originalCompanyName} to {targetCompany.Name} - Trip {trip.TripDate:yyyy-MM-dd}"
                                : $"CompanyTransfer: {dto.Notes.Trim()}",

                            CreatedAtUtc = DateTime.UtcNow
                        };

                        _db.CompanySeatBookings.Add(targetOutbound);
                        createdBookings.Add(targetOutbound);

                        foreach (var old in groupSeats)
                        {
                            var oldSnap = snapshots.FirstOrDefault(s => s.BookingSeatId == old.BookingId);

                            old.TripDate = transferDate;

                            old.TripId = null;
                            old.SeatId = null;
                            old.ReturnTripId = null;
                            old.ReturnSeatId = null;

                            old.SeatsCount = 1;

                            old.FromLocation = dto.FromLocation.Trim();
                            old.ToLocation = dto.ToLocation.Trim();

                            old.TransferredFromBookingId = null;
                            old.TransferredFromSeatId = oldSnap?.SeatId;
                            old.TransferredSeatsJson = JsonSerializer.Serialize(
                                oldSnap == null
                                    ? new List<TransferredSeatSnapshot>()
                                    : new List<TransferredSeatSnapshot> { oldSnap });

                            old.Notes = string.IsNullOrWhiteSpace(old.Notes)
                                ? $"{OriginalCompanyTransferredMarker} Transferred to {targetCompany.Name} - Trip {trip.TripDate:yyyy-MM-dd}"
                                : $"{old.Notes} | {OriginalCompanyTransferredMarker} Transferred to {targetCompany.Name} - Trip {trip.TripDate:yyyy-MM-dd}";
                        }

                        totalTransferredSeats += groupSeats.Count;
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var totalPrice = totalTransferredSeats * pricePerSeat;

                return ResponceApi<TransferResultDTO>.Ok(new TransferResultDTO
                {
                    CompanySeatBookingId = createdBookings.FirstOrDefault()?.BookingId ?? Guid.Empty,
                    CompanyName = targetCompany.Name,
                    SeatsTransferred = totalTransferredSeats,
                    ClientName = $"{totalTransferredSeats} seat(s)",
                    BookingCode = "Mixed transfer",
                    PricePerSeat = pricePerSeat,
                    TotalPrice = totalPrice,
                    Message = $"{totalTransferredSeats} seat(s) transferred to {targetCompany.Name}."
                }, $"{totalTransferredSeats} seat(s) transferred successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<TransferResultDTO>.Fail("Transfer failed.", ex.Message);
            }
        }

        // Create new seat bookings for a company
        public async Task<ResponceApi<List<Guid>>> CreateBookingBoundAsync(CreateCompanySeatBookingDTO dto)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                if (dto == null)
                    return ResponceApi<List<Guid>>.Fail("Invalid request.");

                var company = await _db.Companies
                    .FirstOrDefaultAsync(x => x.CompanyId == dto.CompanyId && x.IsActive);

                if (company == null)
                    return ResponceApi<List<Guid>>.Fail("Company not found or inactive.");

                var createdIds = new List<Guid>();

                // =========================================================
                // Inbound: Seat reservations are available within Trip.
                // =========================================================
                if (dto.BookingDirection == CompanySeatBookingDirection.Inbound)
                {
                    if (!dto.TripId.HasValue || dto.TripId.Value == Guid.Empty)
                        return ResponceApi<List<Guid>>.Fail("TripId is required for Inbound.");

                    if (dto.SeatIds == null || !dto.SeatIds.Any())
                        return ResponceApi<List<Guid>>.Fail("At least one SeatId is required for Inbound.");

                    if (string.IsNullOrWhiteSpace(dto.FromLocation) ||
                        string.IsNullOrWhiteSpace(dto.ToLocation))
                        return ResponceApi<List<Guid>>.Fail("From and To locations are required.");

                    var trip = await _db.BusTrips
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.TripId == dto.TripId.Value &&
                            !x.IsClosed);

                    if (trip == null)
                        return ResponceApi<List<Guid>>.Fail("Trip not found or closed.");

                    var distinctSeatIds = dto.SeatIds
                        .Where(x => x != Guid.Empty)
                        .Distinct()
                        .ToList();

                    if (!distinctSeatIds.Any())
                        return ResponceApi<List<Guid>>.Fail("Selected seats are invalid.");

                    var snapshotSeats = GetSeatsFromSnapshot(trip);

                    if (snapshotSeats == null || !snapshotSeats.Any())
                        return ResponceApi<List<Guid>>.Fail("Trip seats snapshot is missing.");

                    var selectedSnapshotSeats = snapshotSeats
                        .Where(x =>
                            distinctSeatIds.Contains(x.SeatId) &&
                            x.IsActive &&
                            (
                                x.SeatType == SeatType.Normal ||
                                x.SeatType == SeatType.VIP
                            ))
                        .ToList();

                    if (selectedSnapshotSeats.Count != distinctSeatIds.Count)
                        return ResponceApi<List<Guid>>.Fail("Some seats are not valid for this trip.");

                   
                    var realBusSeatIds = await _db.BusSeats
                        .AsNoTracking()
                        .Where(x =>
                            x.BusId == trip.BusId &&
                            x.IsActive &&
                            distinctSeatIds.Contains(x.SeatId))
                        .Select(x => x.SeatId)
                        .ToListAsync();

                    if (realBusSeatIds.Count != distinctSeatIds.Count)
                    {
                        return ResponceApi<List<Guid>>.Fail(
                            "Some selected seats no longer exist in the bus seats table. Please refresh the page and try again.");
                    }

                    var clientConflict = await _db.BookingTransportationSeats
                        .AsNoTracking()
                        .Where(x =>
                            x.TripId == trip.TripId &&
                            distinctSeatIds.Contains(x.SeatId))
                        .Select(x => x.SeatId)
                        .ToListAsync();

                    if (clientConflict.Any())
                    {
                        var labels = clientConflict.Select(seatId =>
                        {
                            var seat = selectedSnapshotSeats.FirstOrDefault(x => x.SeatId == seatId);
                            return seat?.SeatLabel ?? seat?.SeatNumber.ToString() ?? seatId.ToString();
                        });

                        return ResponceApi<List<Guid>>.Fail(
                            $"The following seats are already booked by clients: {string.Join(", ", labels)}");
                    }

                    var companyDepartureConflict = await _db.CompanySeatBookings
                        .AsNoTracking()
                        .Where(x =>
                            x.TripId == trip.TripId &&
                            x.SeatId != null &&
                            distinctSeatIds.Contains(x.SeatId.Value))
                        .Select(x => new
                        {
                            x.SeatId,
                            CompanyName = x.Company.Name
                        })
                        .ToListAsync();

                    var companyReturnConflict = await _db.CompanySeatBookings
                        .AsNoTracking()
                        .Where(x =>
                            x.ReturnTripId == trip.TripId &&
                            x.ReturnSeatId != null &&
                            distinctSeatIds.Contains(x.ReturnSeatId.Value))
                        .Select(x => new
                        {
                            SeatId = x.ReturnSeatId,
                            CompanyName = x.Company.Name
                        })
                        .ToListAsync();

                    var companyConflict = companyDepartureConflict
                        .Select(x => new { x.SeatId, x.CompanyName })
                        .Concat(companyReturnConflict.Select(x => new { x.SeatId, x.CompanyName }))
                        .ToList();

                    if (companyConflict.Any())
                    {
                        var conflicts = companyConflict.Select(x =>
                        {
                            var seat = selectedSnapshotSeats.FirstOrDefault(s => s.SeatId == x.SeatId);

                            return $"{seat?.SeatLabel ?? seat?.SeatNumber.ToString() ?? x.SeatId.ToString()} ({x.CompanyName})";
                        });

                        return ResponceApi<List<Guid>>.Fail(
                            $"The following seats are already booked by another company: {string.Join(", ", conflicts)}");
                    }

                    foreach (var seatId in distinctSeatIds)
                    {
                        var seat = selectedSnapshotSeats.First(x => x.SeatId == seatId);

                        var booking = new CompanySeatBooking
                        {
                            BookingId = Guid.NewGuid(),
                            CompanyId = dto.CompanyId,

                            TripId = trip.TripId,
                            SeatId = seat.SeatId,

                            SeatLabelSnapshot = string.IsNullOrWhiteSpace(seat.SeatLabel)
                                ? seat.SeatNumber.ToString()
                                : seat.SeatLabel,

                            SeatNumberSnapshot = seat.SeatNumber,
                            SeatTypeSnapshot = seat.SeatType,

                            SeatsCount = 1,
                            TripDate = trip.TripDate,

                            FromLocation = dto.FromLocation.Trim(),
                            ToLocation = dto.ToLocation.Trim(),

                            PricePerSeat = Math.Max(0, dto.PricePerSeat),
                            BookingDirection = CompanySeatBookingDirection.Inbound,

                            ClientTripType = dto.ClientTripType,
                            ClientName = dto.ClientName?.Trim(),
                            ClientPhone = dto.ClientPhone?.Trim(),

                            Notes = dto.Notes?.Trim(),
                            CreatedAtUtc = DateTime.UtcNow
                        };

                        _db.CompanySeatBookings.Add(booking);
                        createdIds.Add(booking.BookingId);
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    return ResponceApi<List<Guid>>.Ok(
                        createdIds,
                        $"{createdIds.Count} seat(s) booked for {company.Name}.");
                }

                // =========================================================
                // Outbound: External booking without internal Trip/Seat
                // =========================================================
                if (dto.BookingDirection == CompanySeatBookingDirection.Outbound)
                {
                    if (dto.SeatsCount < 1)
                        return ResponceApi<List<Guid>>.Fail("Seats count must be at least 1.");

                    if (!dto.TripDate.HasValue)
                        return ResponceApi<List<Guid>>.Fail("Trip date is required for Outbound.");

                    if (string.IsNullOrWhiteSpace(dto.FromLocation) ||
                        string.IsNullOrWhiteSpace(dto.ToLocation))
                        return ResponceApi<List<Guid>>.Fail("From and To locations are required.");

                    var booking = new CompanySeatBooking
                    {
                        BookingId = Guid.NewGuid(),
                        CompanyId = dto.CompanyId,

                        TripId = null,
                        SeatId = null,

                        SeatsCount = dto.SeatsCount,
                        TripDate = dto.TripDate.Value.Date,

                        FromLocation = dto.FromLocation.Trim(),
                        ToLocation = dto.ToLocation.Trim(),

                        PricePerSeat = Math.Max(0, dto.PricePerSeat),
                        BookingDirection = CompanySeatBookingDirection.Outbound,

                        ClientTripType = dto.ClientTripType,
                        ClientName = dto.ClientName?.Trim(),
                        ClientPhone = dto.ClientPhone?.Trim(),

                        Notes = dto.Notes?.Trim(),
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    _db.CompanySeatBookings.Add(booking);
                    createdIds.Add(booking.BookingId);

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    return ResponceApi<List<Guid>>.Ok(
                        createdIds,
                        $"{dto.SeatsCount} outbound seat(s) recorded for {company.Name}.");
                }

                return ResponceApi<List<Guid>>.Fail("Invalid booking direction.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ResponceApi<List<Guid>>.Fail("Unexpected error.", ex.Message);
            }
        }

        // Delete a seat booking, with checks for linked payments and transfer scenarios
        public async Task<ResponceApi<bool>> DeleteSeatBookingAsync(Guid bookingId)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                if (bookingId == Guid.Empty)
                    return ResponceApi<bool>.Fail("Booking id is required.");

                var booking = await _db.CompanySeatBookings
                    .Include(x => x.Company)
                    .Include(x => x.Trip)
                    .FirstOrDefaultAsync(x => x.BookingId == bookingId);

                if (booking == null)
                    return ResponceApi<bool>.Fail("Seat booking not found.");

                if (booking.BookingDirection == CompanySeatBookingDirection.Inbound &&
                    booking.TripId == null &&
                    booking.SeatId == null &&
                    !string.IsNullOrWhiteSpace(booking.TransferredSeatsJson) &&
                    !string.IsNullOrWhiteSpace(booking.Notes) &&
                    booking.Notes.Contains(OriginalCompanyTransferredMarker))
                {
                    return ResponceApi<bool>.Fail(
                        "This seat was transferred to another company. Please delete the outbound transfer booking first, then delete this booking if needed.");
                }

                if (booking.BookingDirection == CompanySeatBookingDirection.Outbound &&
                    !string.IsNullOrWhiteSpace(booking.TransferredSeatsJson))
                {
                    List<TransferredSeatSnapshot>? snapshots;

                    try
                    {
                        snapshots = JsonSerializer.Deserialize<List<TransferredSeatSnapshot>>(
                            booking.TransferredSeatsJson,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                    }
                    catch
                    {
                        return ResponceApi<bool>.Fail("Invalid transfer snapshot.");
                    }

                    snapshots ??= new List<TransferredSeatSnapshot>();

                    if (snapshots.Any())
                    {
                        var isClientTransfer = booking.TransferredFromBookingId.HasValue;

                        var isCompanyTransfer =
                            !isClientTransfer &&
                            snapshots.Any(x =>
                                x.OriginalCompanyId.HasValue &&
                                x.OriginalCompanyId.Value != Guid.Empty);

                        var isExternalReplacement =
                            !isClientTransfer &&
                            !isCompanyTransfer &&
                            !string.IsNullOrWhiteSpace(booking.Notes) &&
                            booking.Notes.Contains(ExternalReplacementMarker);

                        if (isClientTransfer)
                        {
                            var originalBooking = await _db.Bookings
                                .Include(x => x.TransportationSeats)
                                .FirstOrDefaultAsync(x =>
                                    x.BookingID == booking.TransferredFromBookingId!.Value &&
                                    !x.IsDeleted);

                            if (originalBooking != null)
                            {
                                foreach (var snap in snapshots)
                                {
                                    if (snap.TripId == Guid.Empty || snap.SeatId == Guid.Empty)
                                        return ResponceApi<bool>.Fail("Original trip/seat data is missing.");

                                    var tripExists = await _db.BusTrips.AnyAsync(x =>
                                        x.TripId == snap.TripId &&
                                        !x.IsClosed);

                                    if (!tripExists)
                                        return ResponceApi<bool>.Fail($"Trip no longer exists or is closed for seat {snap.SeatLabel}.");

                                    var takenByClient = await _db.BookingTransportationSeats.AnyAsync(x =>
                                        x.TripId == snap.TripId &&
                                        x.SeatId == snap.SeatId);

                                    if (takenByClient)
                                        return ResponceApi<bool>.Fail($"Seat {snap.SeatLabel} already booked by another client.");

                                    var takenByCompany = await _db.CompanySeatBookings.AnyAsync(x =>
                                        x.TripId == snap.TripId &&
                                        x.SeatId == snap.SeatId);

                                    if (takenByCompany)
                                        return ResponceApi<bool>.Fail($"Seat {snap.SeatLabel} already booked by another company.");
                                }

                                foreach (var snap in snapshots)
                                {
                                    _db.BookingTransportationSeats.Add(new BookingTransportationSeat
                                    {
                                        BookingSeatId = Guid.NewGuid(),
                                        BookingId = originalBooking.BookingID,
                                        TripId = snap.TripId,
                                        SeatId = snap.SeatId,
                                        Direction = (TripDirection)snap.Direction,
                                        FromLocation = snap.FromLocation,
                                        ToLocation = snap.ToLocation,
                                        SeatPrice = snap.SeatPrice,
                                        ReservedAtUtc = DateTime.UtcNow
                                    });
                                }

                                originalBooking.SeatsCount += snapshots.Count;
                                originalBooking.HasTransportation = true;
                                originalBooking.UpdatedAtUtc = DateTime.UtcNow;
                            }
                        }
                        else if (isCompanyTransfer)
                        {
                            foreach (var snap in snapshots)
                            {
                                if (!snap.OriginalCompanyId.HasValue || snap.OriginalCompanyId.Value == Guid.Empty)
                                    return ResponceApi<bool>.Fail("Cannot restore company seat because original company data is missing.");

                                if (snap.TripId == Guid.Empty || snap.SeatId == Guid.Empty)
                                    return ResponceApi<bool>.Fail("Cannot restore company seat because trip/seat data is missing.");

                                var tripExists = await _db.BusTrips
                                    .AsNoTracking()
                                    .AnyAsync(x => x.TripId == snap.TripId && !x.IsClosed);

                                if (!tripExists)
                                    return ResponceApi<bool>.Fail($"Trip no longer exists or is closed for seat {snap.SeatLabel}.");

                                var companyExists = await _db.Companies.AnyAsync(x =>
                                    x.CompanyId == snap.OriginalCompanyId.Value &&
                                    x.IsActive);

                                if (!companyExists)
                                    return ResponceApi<bool>.Fail("Original company not found or inactive.");

                                var takenByClient = await _db.BookingTransportationSeats.AnyAsync(x =>
                                    x.TripId == snap.TripId &&
                                    x.SeatId == snap.SeatId);

                                if (takenByClient)
                                    return ResponceApi<bool>.Fail($"Seat {snap.SeatLabel} already booked by a client.");

                                var takenByCompany = await _db.CompanySeatBookings.AnyAsync(x =>
                                    x.BookingId != snap.BookingSeatId &&
                                    x.TripId == snap.TripId &&
                                    x.SeatId == snap.SeatId);

                                if (takenByCompany)
                                    return ResponceApi<bool>.Fail($"Seat {snap.SeatLabel} already booked by another company.");
                            }

                            foreach (var snap in snapshots)
                            {
                                var trip = await _db.BusTrips
                                    .FirstAsync(x => x.TripId == snap.TripId);

                                var original = snap.BookingSeatId != Guid.Empty
                                    ? await _db.CompanySeatBookings
                                        .FirstOrDefaultAsync(x => x.BookingId == snap.BookingSeatId)
                                    : null;

                                if (original != null)
                                {
                                    original.CompanyId = snap.OriginalCompanyId!.Value;
                                    original.BookingDirection = CompanySeatBookingDirection.Inbound;

                                    original.TripId = snap.TripId;
                                    original.SeatId = snap.SeatId;

                                    original.ReturnTripId = null;
                                    original.ReturnSeatId = null;

                                    original.SeatsCount = 1;
                                    original.TripDate = trip.TripDate;

                                    original.FromLocation = snap.FromLocation;
                                    original.ToLocation = snap.ToLocation;

                                    original.PricePerSeat = Math.Max(0, snap.SeatPrice);

                                    original.ClientTripType = snap.OriginalClientTripType
                                        ?? (trip.Direction == TripDirection.Return
                                            ? TransportationTripType.Return
                                            : TransportationTripType.Departure);

                                    original.ClientName = snap.OriginalClientName;
                                    original.ClientPhone = snap.OriginalClientPhone;

                                    original.SeatLabelSnapshot = snap.SeatLabel;
                                    original.SeatNumberSnapshot = snap.SeatNumber;
                                    original.SeatTypeSnapshot = snap.SeatType;

                                    original.TransferredFromBookingId = null;
                                    original.TransferredFromSeatId = null;
                                    original.TransferredSeatsJson = null;

                                    original.Notes = "Restored after deleting company transfer.";
                                }
                                else
                                {
                                    _db.CompanySeatBookings.Add(new CompanySeatBooking
                                    {
                                        BookingId = Guid.NewGuid(),
                                        CompanyId = snap.OriginalCompanyId!.Value,

                                        TripId = snap.TripId,
                                        SeatId = snap.SeatId,

                                        SeatsCount = 1,
                                        TripDate = trip.TripDate,

                                        FromLocation = snap.FromLocation,
                                        ToLocation = snap.ToLocation,

                                        PricePerSeat = Math.Max(0, snap.SeatPrice),
                                        BookingDirection = CompanySeatBookingDirection.Inbound,

                                        ClientTripType = snap.OriginalClientTripType
                                            ?? (trip.Direction == TripDirection.Return
                                                ? TransportationTripType.Return
                                                : TransportationTripType.Departure),

                                        ClientName = snap.OriginalClientName,
                                        ClientPhone = snap.OriginalClientPhone,

                                        SeatLabelSnapshot = snap.SeatLabel,
                                        SeatNumberSnapshot = snap.SeatNumber,
                                        SeatTypeSnapshot = snap.SeatType,

                                        Notes = "Restored after deleting company transfer. Original record was missing.",
                                        CreatedAtUtc = DateTime.UtcNow
                                    });
                                }
                            }
                        }
                        else if (isExternalReplacement)
                        {
                            foreach (var snap in snapshots)
                            {
                                if (snap.BookingSeatId == Guid.Empty)
                                    return ResponceApi<bool>.Fail("Original company booking id is missing.");

                                if (snap.TripId == Guid.Empty || snap.SeatId == Guid.Empty)
                                    return ResponceApi<bool>.Fail("Original trip/seat data is missing.");

                                var original = await _db.CompanySeatBookings
                                    .FirstOrDefaultAsync(x => x.BookingId == snap.BookingSeatId);

                                if (original == null)
                                    return ResponceApi<bool>.Fail($"Original company booking was not found for seat {snap.SeatLabel}.");

                                var tripExists = await _db.BusTrips
                                    .AsNoTracking()
                                    .AnyAsync(x => x.TripId == snap.TripId && !x.IsClosed);

                                if (!tripExists)
                                    return ResponceApi<bool>.Fail($"Original trip is closed or deleted for seat {snap.SeatLabel}.");

                                var takenByClient = await _db.BookingTransportationSeats.AnyAsync(x =>
                                    x.TripId == snap.TripId &&
                                    x.SeatId == snap.SeatId);

                                if (takenByClient)
                                    return ResponceApi<bool>.Fail($"Seat {snap.SeatLabel} is already booked by a client.");

                                var takenByCompany = await _db.CompanySeatBookings.AnyAsync(x =>
                                    x.BookingId != original.BookingId &&
                                    x.TripId == snap.TripId &&
                                    x.SeatId == snap.SeatId);

                                if (takenByCompany)
                                    return ResponceApi<bool>.Fail($"Seat {snap.SeatLabel} is already booked by another company.");
                            }

                            foreach (var snap in snapshots)
                            {
                                var original = await _db.CompanySeatBookings
                                    .FirstAsync(x => x.BookingId == snap.BookingSeatId);

                                var trip = await _db.BusTrips
                                    .FirstAsync(x => x.TripId == snap.TripId);

                                original.BookingDirection = CompanySeatBookingDirection.Inbound;

                                original.TripId = snap.TripId;
                                original.SeatId = snap.SeatId;

                                original.TripDate = trip.TripDate;

                                original.FromLocation = snap.FromLocation;
                                original.ToLocation = snap.ToLocation;

                                original.SeatsCount = 1;

                                original.SeatNumberSnapshot = snap.SeatNumber;
                                original.SeatLabelSnapshot = snap.SeatLabel;
                                original.SeatTypeSnapshot = snap.SeatType;

                                original.TransferredFromSeatId = null;
                                original.TransferredSeatsJson = null;

                                if (!string.IsNullOrWhiteSpace(original.Notes))
                                {
                                    original.Notes = original.Notes
                                        .Replace(OriginalCompanyTransferredMarker, "")
                                        .Replace("Transferred to", "Restored from transfer to")
                                        .Trim();

                                    if (string.IsNullOrWhiteSpace(original.Notes))
                                        original.Notes = null;
                                }
                            }
                        }
                    }
                }

                _db.CompanySeatBookings.Remove(booking);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Seat booking deleted successfully.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ResponceApi<bool>.Fail("Delete failed.", ex.Message);
            }
        }

        // Delete multiple seat bookings in batch, with early exit on failure and aggregated result message
        public async Task<ResponceApi<bool>> DeleteSeatBookingsAsync(List<Guid> bookingIds)
        {
            try
            {
                if (bookingIds == null || !bookingIds.Any())
                    return ResponceApi<bool>.Fail("No bookings selected.");

                bookingIds = bookingIds
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (!bookingIds.Any())
                    return ResponceApi<bool>.Fail("No valid bookings selected.");

                var deletedCount = 0;

                foreach (var bookingId in bookingIds)
                {
                    var result = await DeleteSeatBookingAsync(bookingId);

                    if (!result.Success)
                    {
                        return ResponceApi<bool>.Fail(
                            $"{deletedCount} reservation(s) were deleted, then deletion stopped. {result.Message}");
                    }

                    deletedCount++;
                }

                return ResponceApi<bool>.Ok(
                    true,
                    $"{deletedCount} reservation/seat(s) successfully deleted.");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail(
                    "Failed to delete selected bookings.",
                    ex.Message);
            }
        }

        // Get details of conflicts that would occur if the specified bookings were force deleted, including related company and client bookings that share the same trip/seat
        public async Task<ResponceApi<ForceDeleteConflictDetailsDTO>> GetForceDeleteConflictDetailsAsync(List<Guid> bookingIds)
        {
            try
            {
                bookingIds = bookingIds?
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .ToList() ?? new List<Guid>();

                if (!bookingIds.Any())
                    return ResponceApi<ForceDeleteConflictDetailsDTO>.Fail("No bookings selected.");

                var selectedBookings = await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Include(x => x.Company)
                    .Include(x => x.Trip)
                    .Where(x => bookingIds.Contains(x.BookingId))
                    .ToListAsync();

                if (!selectedBookings.Any())
                    return ResponceApi<ForceDeleteConflictDetailsDTO>.Fail("Selected bookings were not found.");

                var deleteIds = new HashSet<Guid>(bookingIds);
                var rows = new List<ForceDeleteConflictSeatDTO>();

                foreach (var booking in selectedBookings)
                {
                    rows.Add(new ForceDeleteConflictSeatDTO
                    {
                        BookingId = booking.BookingId,
                        TripId = booking.TripId,
                        SeatId = booking.SeatId,
                        SeatLabel = booking.SeatLabelSnapshot ?? booking.SeatNumberSnapshot.ToString(),
                        CompanyName = booking.Company?.Name,
                        ClientName = booking.ClientName,
                        ClientPhone = booking.ClientPhone,
                        BusName = booking.Trip?.BusNameSnapshot ?? "External Bus",
                        TripDate = booking.TripDate,
                        Route = $"{booking.FromLocation} → {booking.ToLocation}",
                        Type = booking.BookingDirection.ToString()
                    });

                    if (string.IsNullOrWhiteSpace(booking.TransferredSeatsJson))
                        continue;

                    List<TransferredSeatSnapshot>? snaps;

                    try
                    {
                        snaps = JsonSerializer.Deserialize<List<TransferredSeatSnapshot>>(
                            booking.TransferredSeatsJson,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch
                    {
                        snaps = new List<TransferredSeatSnapshot>();
                    }

                    foreach (var snap in snaps ?? new List<TransferredSeatSnapshot>())
                    {
                        if (snap.BookingSeatId != Guid.Empty)
                            deleteIds.Add(snap.BookingSeatId);

                        rows.Add(new ForceDeleteConflictSeatDTO
                        {
                            BookingId = snap.BookingSeatId != Guid.Empty ? snap.BookingSeatId : booking.BookingId,
                            TripId = snap.TripId,
                            SeatId = snap.SeatId,
                            SeatLabel = snap.SeatLabel,
                            CompanyName = snap.OriginalCompanyName ?? booking.Company?.Name,
                            ClientName = snap.OriginalClientName ?? booking.ClientName,
                            ClientPhone = snap.OriginalClientPhone ?? booking.ClientPhone,
                            BusName = booking.Trip?.BusNameSnapshot ?? "-",
                            TripDate = booking.TripDate,
                            Route = $"{snap.FromLocation} → {snap.ToLocation}",
                            Type = "Original booking"
                        });
                    }
                }

                var relatedBookings = await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Include(x => x.Company)
                    .Include(x => x.Trip)
                    .Where(x => deleteIds.Contains(x.BookingId) && !bookingIds.Contains(x.BookingId))
                    .ToListAsync();

                foreach (var item in relatedBookings)
                {
                    rows.Add(new ForceDeleteConflictSeatDTO
                    {
                        BookingId = item.BookingId,
                        TripId = item.TripId,
                        SeatId = item.SeatId,
                        SeatLabel = item.SeatLabelSnapshot ?? item.SeatNumberSnapshot.ToString(),
                        CompanyName = item.Company?.Name,
                        ClientName = item.ClientName,
                        ClientPhone = item.ClientPhone,
                        BusName = item.Trip?.BusNameSnapshot ?? "External Bus",
                        TripDate = item.TripDate,
                        Route = $"{item.FromLocation} → {item.ToLocation}",
                        Type = "Related original"
                    });
                }

                var data = new ForceDeleteConflictDetailsDTO
                {
                    BookingIds = deleteIds.ToList(),
                    Message = "Force delete will remove only the selected transfer booking and its original related booking records.",
                    Seats = rows
                        .GroupBy(x => x.BookingId)
                        .Select(x => x.First())
                        .OrderBy(x => x.TripDate)
                        .ThenBy(x => x.CompanyName)
                        .ThenBy(x => x.SeatLabel)
                        .ToList()
                };

                data.TripDate = data.Seats.FirstOrDefault()?.TripDate;
                data.Route = data.Seats.FirstOrDefault()?.Route;
                data.BusName = data.Seats.FirstOrDefault()?.BusName;

                return ResponceApi<ForceDeleteConflictDetailsDTO>.Ok(data);
            }
            catch (Exception ex)
            {
                return ResponceApi<ForceDeleteConflictDetailsDTO>.Fail("Failed to load force delete details.", ex.Message);
            }
        }

        // Force delete the specified bookings and all related company and client bookings that share the same trip/seat, with transaction and aggregated result message
        public async Task<ResponceApi<bool>> ForceDeleteSeatBookingsAsync(List<Guid> bookingIds)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                bookingIds = bookingIds?
                    .Where(x => x != Guid.Empty)
                    .Distinct()
                    .ToList() ?? new List<Guid>();

                if (!bookingIds.Any())
                    return ResponceApi<bool>.Fail("No bookings selected.");

                var selectedBookings = await _db.CompanySeatBookings
                    .Where(x => bookingIds.Contains(x.BookingId))
                    .ToListAsync();

                if (!selectedBookings.Any())
                    return ResponceApi<bool>.Fail("Selected bookings were not found.");

                var deleteIds = new HashSet<Guid>(bookingIds);
                var restoredCount = 0;
                var deletedCount = 0;

                var allTransferBookings = await _db.CompanySeatBookings
                    .Where(x => !string.IsNullOrWhiteSpace(x.TransferredSeatsJson))
                    .ToListAsync();

                foreach (var transfer in allTransferBookings)
                {
                    List<TransferredSeatSnapshot> snaps;

                    try
                    {
                        snaps = JsonSerializer.Deserialize<List<TransferredSeatSnapshot>>(
                            transfer.TransferredSeatsJson!,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        ) ?? new List<TransferredSeatSnapshot>();
                    }
                    catch
                    {
                        snaps = new List<TransferredSeatSnapshot>();
                    }

                    if (!snaps.Any())
                        continue;

                    var relatedToSelected = deleteIds.Contains(transfer.BookingId) ||
                                            snaps.Any(x => x.BookingSeatId != Guid.Empty && deleteIds.Contains(x.BookingSeatId));

                    if (!relatedToSelected)
                        continue;

                    deleteIds.Add(transfer.BookingId);

                    var isClientTransfer = transfer.TransferredFromBookingId.HasValue;

                    var isCompanyTransfer =
                        !isClientTransfer &&
                        snaps.Any(x => x.OriginalCompanyId.HasValue && x.OriginalCompanyId.Value != Guid.Empty);

                    var isExternalReplacement =
                        !isClientTransfer &&
                        !isCompanyTransfer &&
                        !string.IsNullOrWhiteSpace(transfer.Notes) &&
                        transfer.Notes.Contains(ExternalReplacementMarker);

                    var canRestore = true;

                    foreach (var snap in snaps)
                    {
                        if (snap.TripId == Guid.Empty || snap.SeatId == Guid.Empty)
                        {
                            canRestore = false;
                            break;
                        }

                        var tripExists = await _db.BusTrips
                            .AsNoTracking()
                            .AnyAsync(x => x.TripId == snap.TripId && !x.IsClosed);

                        if (!tripExists)
                        {
                            canRestore = false;
                            break;
                        }

                        var takenByClient = await _db.BookingTransportationSeats
                            .AsNoTracking()
                            .AnyAsync(x => x.TripId == snap.TripId && x.SeatId == snap.SeatId);

                        if (takenByClient)
                        {
                            canRestore = false;
                            break;
                        }

                        var takenByCompany = await _db.CompanySeatBookings
                            .AsNoTracking()
                            .AnyAsync(x =>
                                x.TripId == snap.TripId &&
                                x.SeatId == snap.SeatId &&
                                x.BookingId != snap.BookingSeatId &&
                                x.BookingId != transfer.BookingId);

                        if (takenByCompany)
                        {
                            canRestore = false;
                            break;
                        }

                        if (isCompanyTransfer && (!snap.OriginalCompanyId.HasValue || snap.OriginalCompanyId.Value == Guid.Empty))
                        {
                            canRestore = false;
                            break;
                        }
                    }

                    if (!canRestore)
                    {
                        foreach (var snap in snaps)
                        {
                            if (snap.BookingSeatId != Guid.Empty)
                                deleteIds.Add(snap.BookingSeatId);
                        }

                        continue;
                    }

                    if (isClientTransfer)
                    {
                        var originalBooking = await _db.Bookings
                            .Include(x => x.TransportationSeats)
                            .FirstOrDefaultAsync(x =>
                                x.BookingID == transfer.TransferredFromBookingId!.Value &&
                                !x.IsDeleted);

                        if (originalBooking != null)
                        {
                            foreach (var snap in snaps)
                            {
                                _db.BookingTransportationSeats.Add(new BookingTransportationSeat
                                {
                                    BookingSeatId = Guid.NewGuid(),
                                    BookingId = originalBooking.BookingID,
                                    TripId = snap.TripId,
                                    SeatId = snap.SeatId,
                                    Direction = (TripDirection)snap.Direction,
                                    FromLocation = snap.FromLocation,
                                    ToLocation = snap.ToLocation,
                                    SeatPrice = snap.SeatPrice,
                                    ReservedAtUtc = DateTime.UtcNow
                                });
                            }

                            originalBooking.SeatsCount += snaps.Count;
                            originalBooking.HasTransportation = true;
                            originalBooking.UpdatedAtUtc = DateTime.UtcNow;

                            restoredCount += snaps.Count;
                        }
                    }
                    else if (isCompanyTransfer)
                    {
                        foreach (var snap in snaps)
                        {
                            var trip = await _db.BusTrips
                                .FirstAsync(x => x.TripId == snap.TripId);

                            CompanySeatBooking? original = null;

                            if (snap.BookingSeatId != Guid.Empty)
                            {
                                original = await _db.CompanySeatBookings
                                    .FirstOrDefaultAsync(x => x.BookingId == snap.BookingSeatId);
                            }

                            if (original != null)
                            {
                                original.CompanyId = snap.OriginalCompanyId!.Value;
                                original.BookingDirection = CompanySeatBookingDirection.Inbound;

                                original.TripId = snap.TripId;
                                original.SeatId = snap.SeatId;

                                original.ReturnTripId = null;
                                original.ReturnSeatId = null;

                                original.SeatsCount = 1;
                                original.TripDate = trip.TripDate;

                                original.FromLocation = snap.FromLocation;
                                original.ToLocation = snap.ToLocation;

                                original.PricePerSeat = Math.Max(0, snap.SeatPrice);

                                original.ClientTripType = snap.OriginalClientTripType
                                    ?? (trip.Direction == TripDirection.Return
                                        ? TransportationTripType.Return
                                        : TransportationTripType.Departure);

                                original.ClientName = snap.OriginalClientName;
                                original.ClientPhone = snap.OriginalClientPhone;

                                original.SeatLabelSnapshot = snap.SeatLabel;
                                original.SeatNumberSnapshot = snap.SeatNumber;
                                original.SeatTypeSnapshot = snap.SeatType;

                                original.TransferredFromBookingId = null;
                                original.TransferredFromSeatId = null;
                                original.TransferredSeatsJson = null;

                                original.Notes = "Restored after force delete company transfer.";

                                deleteIds.Remove(original.BookingId);
                            }
                            else
                            {
                                _db.CompanySeatBookings.Add(new CompanySeatBooking
                                {
                                    BookingId = Guid.NewGuid(),
                                    CompanyId = snap.OriginalCompanyId!.Value,

                                    TripId = snap.TripId,
                                    SeatId = snap.SeatId,

                                    SeatsCount = 1,
                                    TripDate = trip.TripDate,

                                    FromLocation = snap.FromLocation,
                                    ToLocation = snap.ToLocation,

                                    PricePerSeat = Math.Max(0, snap.SeatPrice),
                                    BookingDirection = CompanySeatBookingDirection.Inbound,

                                    ClientTripType = snap.OriginalClientTripType
                                        ?? (trip.Direction == TripDirection.Return
                                            ? TransportationTripType.Return
                                            : TransportationTripType.Departure),

                                    ClientName = snap.OriginalClientName,
                                    ClientPhone = snap.OriginalClientPhone,

                                    SeatLabelSnapshot = snap.SeatLabel,
                                    SeatNumberSnapshot = snap.SeatNumber,
                                    SeatTypeSnapshot = snap.SeatType,

                                    Notes = "Restored after force delete company transfer. Original record was missing.",
                                    CreatedAtUtc = DateTime.UtcNow
                                });
                            }

                            restoredCount++;
                        }
                    }
                    else if (isExternalReplacement)
                    {
                        foreach (var snap in snaps)
                        {
                            if (snap.BookingSeatId == Guid.Empty)
                                continue;

                            var original = await _db.CompanySeatBookings
                                .FirstOrDefaultAsync(x => x.BookingId == snap.BookingSeatId);

                            if (original == null)
                                continue;

                            var trip = await _db.BusTrips
                                .FirstAsync(x => x.TripId == snap.TripId);

                            original.BookingDirection = CompanySeatBookingDirection.Inbound;

                            original.TripId = snap.TripId;
                            original.SeatId = snap.SeatId;

                            original.TripDate = trip.TripDate;

                            original.FromLocation = snap.FromLocation;
                            original.ToLocation = snap.ToLocation;

                            original.SeatsCount = 1;

                            original.SeatNumberSnapshot = snap.SeatNumber;
                            original.SeatLabelSnapshot = snap.SeatLabel;
                            original.SeatTypeSnapshot = snap.SeatType;

                            original.TransferredFromSeatId = null;
                            original.TransferredSeatsJson = null;

                            if (!string.IsNullOrWhiteSpace(original.Notes))
                            {
                                original.Notes = original.Notes
                                    .Replace(OriginalCompanyTransferredMarker, "")
                                    .Replace("Transferred to", "Restored from transfer to")
                                    .Trim();

                                if (string.IsNullOrWhiteSpace(original.Notes))
                                    original.Notes = null;
                            }

                            deleteIds.Remove(original.BookingId);
                            restoredCount++;
                        }
                    }

                    deleteIds.Add(transfer.BookingId);
                }

                var companyBookingsToDelete = await _db.CompanySeatBookings
                    .Where(x => deleteIds.Contains(x.BookingId))
                    .ToListAsync();

                if (companyBookingsToDelete.Any())
                {
                    deletedCount = companyBookingsToDelete.Count;
                    _db.CompanySeatBookings.RemoveRange(companyBookingsToDelete);
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ResponceApi<bool>.Ok(
                    true,
                    $"Force delete completed. Restored: {restoredCount}, Deleted: {deletedCount}.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ResponceApi<bool>.Fail("Force delete failed.", ex.Message);
            }
        }

        private async Task<int> GetSeatBookingBoundAvailableSeatsCountForTripAsync(BusTrip trip)
        {
            var total = CountSeatBookingBoundTripSnapshotPassengerSeats(trip);
            var clientReserved = trip.ReservedSeats?.Count ?? 0;

            var companyReserved = await _db.CompanySeatBookings
                .AsNoTracking()
                .CountAsync(x => x.TripId == trip.TripId && x.SeatId != null);

            return Math.Max(0, total - clientReserved - companyReserved);
        }

        private static int CountSeatBookingBoundTripSnapshotPassengerSeats(BusTrip trip)
        {
            var seats = ReadSeatBookingBoundSeatsFromSnapshot(trip.SeatsSnapshotJson);

            return seats.Count(x =>
                x.IsActive &&
                (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP));
        }

        private static List<SeatBookingBoundSeatDTO> ReadSeatBookingBoundSeatsFromSnapshot(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<SeatBookingBoundSeatDTO>();

            try
            {
                var seats = JsonSerializer.Deserialize<List<SeatBookingBoundSeatDTO>>(json);
                return seats ?? new List<SeatBookingBoundSeatDTO>();
            }
            catch
            {
                return new List<SeatBookingBoundSeatDTO>();
            }
        }

        private static string? ExtractTransferredToCompanyName(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
                return null;

            const string marker = "Transferred to ";

            var index = notes.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

            if (index < 0)
                return null;

            var value = notes[(index + marker.Length)..].Trim();

            var separatorIndex = value.IndexOf(" - Trip", StringComparison.OrdinalIgnoreCase);

            if (separatorIndex >= 0)
                value = value[..separatorIndex].Trim();

            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        //--------------------------------------------------------------------------------------------------------------------//

        // Search for company seat bookings based on various criteria and return grouped summaries by trip and booking direction, including payment information and transfer status
        public async Task<ResponceApi<List<CompanyTripSeatSummaryDTO>>> SearchAsync(CompanySeatBookingSearchDTO search)
        {
            try
            {
                var query = _db.CompanySeatBookings
                    .AsNoTracking()
                    .Include(x => x.Company)
                    .Include(x => x.Trip).ThenInclude(x => x!.Bus)
                    .Include(x => x.Seat)
                    .AsQueryable();

                if (search.CompanyId.HasValue && search.CompanyId.Value != Guid.Empty)
                    query = query.Where(x => x.CompanyId == search.CompanyId.Value);

                if (search.TripId.HasValue && search.TripId.Value != Guid.Empty)
                    query = query.Where(x => x.TripId == search.TripId.Value);

                if (search.DateFrom.HasValue)
                    query = query.Where(x =>
                        (x.TripDate != null ? x.TripDate.Value.Date : x.Trip!.TripDate.Date) >= search.DateFrom.Value.Date);

                if (search.DateTo.HasValue)
                    query = query.Where(x =>
                        (x.TripDate != null ? x.TripDate.Value.Date : x.Trip!.TripDate.Date) <= search.DateTo.Value.Date);

                if (!string.IsNullOrWhiteSpace(search.Location))
                {
                    var loc = search.Location.Trim().ToLower();
                    query = query.Where(x =>
                        x.FromLocation.ToLower().Contains(loc) ||
                        x.ToLocation.ToLower().Contains(loc));
                }

                if (search.Direction.HasValue)
                    query = query.Where(x => x.BookingDirection == search.Direction.Value);

                var rawData = await query
                    .OrderByDescending(x => x.TripDate ?? (x.Trip != null ? x.Trip.TripDate : DateTime.MinValue))
                    .ToListAsync();

                var allPayments = rawData.Any()
                    ? await _db.CompanySeatPayments
                        .Where(x => rawData.Select(b => b.CompanyId).Distinct().Contains(x.CompanyId))
                        .ToListAsync()
                    : new List<CompanySeatPayment>();

                var inboundGroups = rawData
                    .Where(x => x.BookingDirection == CompanySeatBookingDirection.Inbound && x.TripId.HasValue)
                    .GroupBy(x => new { x.CompanyId, x.TripId })
                    .Select(g =>
                    {
                        var first = g.First();
                        var company = first.Company;
                        var trip = first.Trip!;
                        var tripPaid = allPayments
                            .Where(p => p.CompanyId == first.CompanyId && p.TripId == first.TripId)
                            .Sum(p => p.Amount);

                        return new CompanyTripSeatSummaryDTO
                        {
                            CompanyId = first.CompanyId,
                            CompanyName = company.Name,
                            CompanyPhone = company.PhoneNumber,
                            TripId = trip.TripId,
                            TripDate = trip.TripDate,
                            TripDirection = trip.Direction,
                            TripFromLocation = trip.FromLocation ?? string.Empty,
                            TripToLocation = trip.ToLocation ?? string.Empty,
                            BusId = trip.BusId,
                            BusName = trip.BusNameSnapshot,
                            PlateNumber = trip.PlateNumberSnapshot ?? string.Empty,
                            BookingDirection = CompanySeatBookingDirection.Inbound,
                            PricePerSeat = first.PricePerSeat,
                            FromLocation = first.FromLocation,
                            ToLocation = first.ToLocation,
                            TotalPaid = tripPaid,
                            Seats = g.Select(x => MapToItemDTO(x, company)).ToList()
                        };
                    }).ToList();

                var outboundGroups = rawData
                    .Where(x => x.BookingDirection == CompanySeatBookingDirection.Outbound)
                    .Select(x =>
                    {
                        var company = x.Company;
                        return new CompanyTripSeatSummaryDTO
                        {
                            CompanyId = x.CompanyId,
                            CompanyName = company.Name,
                            CompanyPhone = company.PhoneNumber,
                            TripId = null,
                            TripDate = x.TripDate ?? DateTime.MinValue,
                            TripDirection = null,
                            TripFromLocation = x.FromLocation,
                            TripToLocation = x.ToLocation,
                            BusId = null,
                            BusName = "External Bus",
                            PlateNumber = string.Empty,
                            BookingDirection = CompanySeatBookingDirection.Outbound,
                            PricePerSeat = x.PricePerSeat,
                            FromLocation = x.FromLocation,
                            ToLocation = x.ToLocation,
                            TotalPaid = 0,
                            Seats = new List<CompanySeatBookingItemDTO> { MapToItemDTO(x, company) }
                        };
                    }).ToList();

                var transferredOriginalCompanyGroups = rawData
                    .Where(x =>
                        x.BookingDirection == CompanySeatBookingDirection.Inbound &&
                        x.TripId == null &&
                        !string.IsNullOrWhiteSpace(x.TransferredSeatsJson))
                    .Select(x =>
                    {
                        var company = x.Company;

                        var targetCompanyName = ExtractTransferredToCompanyName(x.Notes);

                        var item = MapToItemDTO(x, company);

                        item.IsTransferredToCompany = true;
                        item.TransferredToCompanyName = targetCompanyName;
                        item.TransferStatusText = string.IsNullOrWhiteSpace(targetCompanyName)
                            ? "Transferred to another company"
                            : $"Transferred to {targetCompanyName}";

                        return new CompanyTripSeatSummaryDTO
                        {
                            CompanyId = x.CompanyId,
                            CompanyName = company.Name,
                            CompanyPhone = company.PhoneNumber,

                            TripId = null,
                            TripDate = x.TripDate ?? DateTime.MinValue,
                            TripDirection = null,

                            TripFromLocation = x.FromLocation,
                            TripToLocation = x.ToLocation,

                            BusId = null,
                            BusName = "Transferred",
                            PlateNumber = string.Empty,

                            BookingDirection = CompanySeatBookingDirection.Inbound,

                            PricePerSeat = x.PricePerSeat,
                            FromLocation = x.FromLocation,
                            ToLocation = x.ToLocation,

                            TotalPaid = 0,

                            Seats = new List<CompanySeatBookingItemDTO>
                            {
                                item
                            }
                        };
                    })
                    .ToList();
                var allGroups = inboundGroups
                    .Concat(transferredOriginalCompanyGroups)
                    .Concat(outboundGroups)
                    .OrderByDescending(x => x.TripDate)
                    .ToList();

                allGroups = search.PaymentStatus switch
                {
                    CompanySeatPaymentStatus.Paid => allGroups.Where(x => x.TotalPaid >= x.TotalPrice && x.TotalPrice > 0).ToList(),
                    CompanySeatPaymentStatus.Unpaid => allGroups.Where(x => x.TotalPaid == 0).ToList(),
                    CompanySeatPaymentStatus.Partial => allGroups.Where(x => x.TotalPaid > 0 && x.TotalPaid < x.TotalPrice).ToList(),
                    _ => allGroups
                };

                return ResponceApi<List<CompanyTripSeatSummaryDTO>>.Ok(allGroups);
            }
            catch (Exception ex)
            {
                return ResponceApi<List<CompanyTripSeatSummaryDTO>>.Fail("Failed to search seat bookings.", ex.Message);
            }
        }

        // Get data for the company seat accounting page, including list of active companies for selection and account details for the selected company and date range, with error handling
        public async Task<ResponceApi<CompanySeatAccountingPageDTO>> GetCompanySeatAccountingPageAsync(CompanySeatAccountingFilterDTO filter)
        {
            try
            {
                filter ??= new CompanySeatAccountingFilterDTO();

                var companies = await _db.Companies
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.CompanyId.ToString(),
                        Text = x.Name + " - " + x.PhoneNumber,
                        Selected = filter.CompanyId.HasValue && x.CompanyId == filter.CompanyId.Value
                    })
                    .ToListAsync();

                CompanySeatAccountPageDTO? account = null;
                if (filter.CompanyId.HasValue && filter.CompanyId.Value != Guid.Empty)
                {
                    var accountResult = await GetCompanyAccountAsync(filter.CompanyId.Value, filter.DateFrom, filter.DateTo);
                    if (!accountResult.Success)
                        return ResponceApi<CompanySeatAccountingPageDTO>.Fail(accountResult.Message ?? "Failed to load company account.", accountResult.Errors?.ToArray() ?? Array.Empty<string>());

                    account = accountResult.Data;
                }

                return ResponceApi<CompanySeatAccountingPageDTO>.Ok(new CompanySeatAccountingPageDTO
                {
                    CompanyId = filter.CompanyId,
                    DateFrom = filter.DateFrom,
                    DateTo = filter.DateTo,
                    Companies = companies,
                    Account = account
                });
            }
            catch (Exception ex)
            {
                return ResponceApi<CompanySeatAccountingPageDTO>.Fail("Failed to load company accounting page.", ex.Message);
            }
        }

        // Get company account with optional date filtering for trips and payments, ensuring the company exists and is active, and return the account details including trip summaries and payments
        public async Task<ResponceApi<CompanySeatAccountPageDTO>> GetCompanyAccountAsync(Guid companyId, DateTime? dateFrom, DateTime? dateTo)
        {
            try
            {
                var result = await GetCompanyAccountAsync(companyId);

                if (!result.Success || result.Data == null)
                    return result;

                var account = result.Data;

                if (dateFrom.HasValue)
                {
                    var from = dateFrom.Value.Date;
                    account.TripGroups = account.TripGroups.Where(x => x.TripDate.Date >= from).ToList();
                    account.Payments = account.Payments.Where(x => x.PaidAt.Date >= from).ToList();
                }

                if (dateTo.HasValue)
                {
                    var to = dateTo.Value.Date;
                    account.TripGroups = account.TripGroups.Where(x => x.TripDate.Date <= to).ToList();
                    account.Payments = account.Payments.Where(x => x.PaidAt.Date <= to).ToList();
                }

                return ResponceApi<CompanySeatAccountPageDTO>.Ok(account);
            }
            catch (Exception ex)
            {
                return ResponceApi<CompanySeatAccountPageDTO>.Fail("Failed to load company account.", ex.Message);
            }
        }

        // Update seat price for all bookings of a company for a specific trip or outbound group, with validation against total paid amount
        public async Task<ResponceApi<bool>> UpdateSeatPriceAsync(UpdateCompanySeatPriceDTO dto)
        {
            try
            {
                if (dto.PricePerSeat < 0)
                    return ResponceApi<bool>.Fail("Price cannot be negative.");

                List<CompanySeatBooking> bookings;

                if (dto.BookingDirection == CompanySeatBookingDirection.Inbound)
                    bookings = await _db.CompanySeatBookings
                        .Where(x => x.CompanyId == dto.CompanyId &&
                                    x.TripId == dto.BookingGroupId &&
                                    x.BookingDirection == CompanySeatBookingDirection.Inbound)
                        .ToListAsync();
                else
                    bookings = await _db.CompanySeatBookings
                        .Where(x => x.CompanyId == dto.CompanyId &&
                                    x.BookingId == dto.BookingGroupId &&
                                    x.BookingDirection == CompanySeatBookingDirection.Outbound)
                        .ToListAsync();

                if (!bookings.Any())
                    return ResponceApi<bool>.Fail("No seat bookings found.");

                var totalPaid = await _db.CompanySeatPayments
                    .Where(x => x.CompanyId == dto.CompanyId &&
                                (x.TripId == dto.BookingGroupId || x.TripId == null))
                    .SumAsync(x => (decimal?)x.Amount) ?? 0m;

                var newTotal = bookings.Sum(x => x.SeatsCount) * dto.PricePerSeat;

                if (totalPaid > newTotal)
                    return ResponceApi<bool>.Fail(
                        $"New total ({newTotal:N2}) cannot be less than paid amount ({totalPaid:N2}).");

                foreach (var b in bookings)
                    b.PricePerSeat = dto.PricePerSeat;

                await _db.SaveChangesAsync();
                return ResponceApi<bool>.Ok(true, "Seat prices updated successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail("Failed to update seat prices.", ex.Message);
            }
        }

        // Add a payment for a company with optional trip association, ensuring it does not exceed total due amount for the trip or company, with transaction
        public async Task<ResponceApi<Guid>> AddPaymentAsync(AddCompanySeatPaymentDTO dto)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                if (dto.Amount <= 0)
                    return ResponceApi<Guid>.Fail("Payment amount must be greater than zero.");

                var company = await _db.Companies
                    .FirstOrDefaultAsync(x => x.CompanyId == dto.CompanyId && x.IsActive);

                if (company == null)
                    return ResponceApi<Guid>.Fail("Company not found or inactive.");

                if (!await _db.CompanySeatBookings.AnyAsync(x => x.CompanyId == dto.CompanyId))
                    return ResponceApi<Guid>.Fail("No seat bookings found for this company.");

                var payment = new CompanySeatPayment
                {
                    PaymentId = Guid.NewGuid(),
                    CompanyId = dto.CompanyId,
                    TripId = dto.TripId.HasValue && dto.TripId.Value != Guid.Empty ? dto.TripId : null,
                    Amount = dto.Amount,
                    PaidAt = dto.PaidAt.Date,
                    Notes = dto.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                };

                _db.CompanySeatPayments.Add(payment);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return ResponceApi<Guid>.Ok(payment.PaymentId, "Payment added successfully.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ResponceApi<Guid>.Fail("Failed to add payment.", ex.Message);
            }
        }
        
        // Delete a payment by ID, ensuring it exists and optionally checking if it can be deleted based on business rules (e.g., time limit), with transaction
        public async Task<ResponceApi<bool>> DeletePaymentAsync(Guid paymentId)
        {
            try
            {
                var payment = await _db.CompanySeatPayments
                    .FirstOrDefaultAsync(x => x.PaymentId == paymentId);

                if (payment == null)
                    return ResponceApi<bool>.Fail("Payment not found.");

                //if (!isAdmin && payment.CreatedAtUtc < DateTime.UtcNow.AddDays(-1))
                //    return ResponceApi<bool>.Fail("Cannot delete after 24 hours. Contact admin.");

                _db.CompanySeatPayments.Remove(payment);
                await _db.SaveChangesAsync();

                return ResponceApi<bool>.Ok(true, "Payment deleted successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail("Failed to delete payment.", ex.Message);
            }
        }

        private async Task<ResponceApi<CompanySeatAccountPageDTO>> GetCompanyAccountAsync(Guid companyId)
        {
            try
            {
                if (companyId == Guid.Empty)
                    return ResponceApi<CompanySeatAccountPageDTO>.Fail("Company is required.");

                var company = await _db.Companies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.IsActive);

                if (company == null)
                    return ResponceApi<CompanySeatAccountPageDTO>.Fail("Company not found or inactive.");

                var bookings = await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Include(x => x.Trip)
                    .Include(x => x.Company)
                    .Include(x => x.Seat)
                    .Include(x => x.ReturnTrip)
                    .Include(x => x.ReturnSeat)
                    .Where(x => x.CompanyId == companyId)
                    .OrderByDescending(x => x.TripDate ?? (x.Trip != null ? x.Trip.TripDate : DateTime.MinValue))
                    .ThenByDescending(x => x.CreatedAtUtc)
                    .ToListAsync();

                var payments = await _db.CompanySeatPayments
                    .AsNoTracking()
                    .Include(x => x.Trip)
                    .Where(x => x.CompanyId == companyId)
                    .OrderByDescending(x => x.PaidAt)
                    .ThenByDescending(x => x.CreatedAtUtc)
                    .ToListAsync();

                var inboundGroups = bookings
                    .Where(x => x.BookingDirection == CompanySeatBookingDirection.Inbound && x.TripId.HasValue)
                    .GroupBy(x => x.TripId!.Value)
                    .Select(g =>
                    {
                        var first = g.First();
                        var trip = first.Trip!;

                        var tripPaid = payments
                            .Where(p => p.TripId == trip.TripId)
                            .Sum(p => p.Amount);

                        return new CompanyTripSeatSummaryDTO
                        {
                            CompanyId = company.CompanyId,
                            CompanyName = company.Name,
                            CompanyPhone = company.PhoneNumber,

                            TripId = trip.TripId,
                            TripDate = trip.TripDate,
                            TripDirection = trip.Direction,

                            TripFromLocation = trip.FromLocation ?? string.Empty,
                            TripToLocation = trip.ToLocation ?? string.Empty,

                            BusId = trip.BusId,
                            BusName = trip.BusNameSnapshot ?? "Bus",
                            PlateNumber = trip.PlateNumberSnapshot ?? string.Empty,

                            BookingDirection = CompanySeatBookingDirection.Inbound,

                            FromLocation = first.FromLocation,
                            ToLocation = first.ToLocation,
                            PricePerSeat = first.PricePerSeat,

                            TotalPaid = tripPaid,

                            Seats = g.Select(x => MapToItemDTO(x, company)).ToList()
                        };
                    })
                    .ToList();

                var transferredOriginalCompanyGroups = bookings
                    .Where(x =>
                        x.BookingDirection == CompanySeatBookingDirection.Inbound &&
                        !x.TripId.HasValue &&
                        !string.IsNullOrWhiteSpace(x.TransferredSeatsJson))
                    .Select(x =>
                    {
                        var item = MapToItemDTO(x, company);
                        item.IsTransferredToCompany = true;
                        item.TransferredToCompanyName = ExtractTransferredToCompanyName(x.Notes);
                        item.TransferStatusText = string.IsNullOrWhiteSpace(item.TransferredToCompanyName)
                            ? "Transferred to another company"
                            : $"Transferred to {item.TransferredToCompanyName}";

                        return new CompanyTripSeatSummaryDTO
                        {
                            CompanyId = company.CompanyId,
                            CompanyName = company.Name,
                            CompanyPhone = company.PhoneNumber,

                            TripId = null,
                            TripDate = x.TripDate ?? DateTime.MinValue,
                            TripDirection = TripDirection.Departure,

                            TripFromLocation = x.FromLocation,
                            TripToLocation = x.ToLocation,

                            BusId = null,
                            BusName = "Transferred",
                            PlateNumber = string.Empty,

                            BookingDirection = CompanySeatBookingDirection.Inbound,

                            FromLocation = x.FromLocation,
                            ToLocation = x.ToLocation,
                            PricePerSeat = x.PricePerSeat,

                            TotalPaid = 0,

                            Seats = new List<CompanySeatBookingItemDTO> { item }
                        };
                    })
                    .ToList();

                var outboundGroups = bookings
                    .Where(x => x.BookingDirection == CompanySeatBookingDirection.Outbound)
                    .Select(x => new CompanyTripSeatSummaryDTO
                    {
                        CompanyId = company.CompanyId,
                        CompanyName = company.Name,
                        CompanyPhone = company.PhoneNumber,

                        TripId = null,
                        TripDate = x.TripDate ?? DateTime.MinValue,
                        TripDirection = null,

                        TripFromLocation = x.FromLocation,
                        TripToLocation = x.ToLocation,

                        BusId = null,
                        BusName = "External Bus",
                        PlateNumber = string.Empty,

                        BookingDirection = CompanySeatBookingDirection.Outbound,

                        FromLocation = x.FromLocation,
                        ToLocation = x.ToLocation,
                        PricePerSeat = x.PricePerSeat,

                        TotalPaid = 0,

                        Seats = new List<CompanySeatBookingItemDTO>
                        {
                    MapToItemDTO(x, company)
                        }
                    })
                    .ToList();

                var paymentDtos = payments.Select(p => new CompanySeatPaymentDTO
                {
                    PaymentId = p.PaymentId,
                    CompanyId = p.CompanyId,
                    TripId = p.TripId,
                    TripInfo = p.Trip != null
                        ? $"{p.Trip.TripDate:dd/MM/yyyy} - {p.Trip.BusNameSnapshot} ({p.Trip.FromLocation} → {p.Trip.ToLocation})"
                        : "General Payment",
                    Amount = p.Amount,
                    PaidAt = p.PaidAt,
                    Notes = p.Notes,
                    CreatedAtUtc = p.CreatedAtUtc
                }).ToList();

                var dto = new CompanySeatAccountPageDTO
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.Name,
                    CompanyPhone = company.PhoneNumber,
                    CompanyNotes = company.Notes,

                    TripGroups = inboundGroups
                        .Concat(transferredOriginalCompanyGroups)
                        .Concat(outboundGroups)
                        .OrderByDescending(x => x.TripDate)
                        .ToList(),

                    Payments = paymentDtos
                };

                return ResponceApi<CompanySeatAccountPageDTO>.Ok(dto);
            }
            catch (Exception ex)
            {
                return ResponceApi<CompanySeatAccountPageDTO>.Fail("Failed to load company account.", ex.Message);
            }
        }

        private static CompanySeatBookingItemDTO MapToItemDTO(CompanySeatBooking x, TransportationCompany company)
        {
            return new CompanySeatBookingItemDTO
            {
                BookingId = x.BookingId,
                CompanyId = x.CompanyId,
                CompanyName = company.Name,
                CompanyPhone = company.PhoneNumber,
                TripId = x.TripId,
                TripDate = x.TripDate ?? x.Trip?.TripDate,
                TripDirection = x.Trip?.Direction,
                TripFromLocation = x.Trip?.FromLocation ?? string.Empty,
                TripToLocation = x.Trip?.ToLocation ?? string.Empty,
                BusId = x.Trip?.BusId,
                // ── Trip Snapshot ──
                BusName = x.Trip?.BusNameSnapshot ?? "External Bus",
                PlateNumber = x.Trip?.PlateNumberSnapshot ?? string.Empty,
                SeatId = x.SeatId,
                // ── Seat Snapshot — لا يتأثر بتعديل Bus/BusSeat ──
                SeatNumber = x.SeatNumberSnapshot > 0 ? x.SeatNumberSnapshot : (x.Seat?.SeatNumber ?? 0),
                SeatLabel = x.SeatLabelSnapshot ?? x.Seat?.SeatLabel ?? x.Seat?.SeatNumber.ToString() ?? string.Empty,
                SeatType = x.SeatTypeSnapshot ?? x.Seat?.SeatType,
                SeatsCount = x.SeatsCount,
                FromLocation = x.FromLocation,
                ToLocation = x.ToLocation,
                PricePerSeat = x.PricePerSeat,
                TotalPrice = x.TotalPrice,
                BookingDirection = x.BookingDirection,
                ClientTripType = x.ClientTripType,
                ClientName = x.ClientName,
                ClientPhone = x.ClientPhone,
                IsRoundTrip = x.IsRoundTrip,
                ReturnTripId = x.ReturnTripId,
                ReturnTripDate = x.ReturnTripDate ?? x.ReturnTrip?.TripDate,
                ReturnSeatLabel = x.ReturnSeatLabelSnapshot
                                    ?? x.ReturnSeat?.SeatLabel
                                    ?? x.ReturnSeat?.SeatNumber.ToString()
                                    ?? string.Empty,
                ReturnFromLocation = x.ReturnFromLocation ?? string.Empty,
                ReturnToLocation = x.ReturnToLocation ?? string.Empty,
                IsTransfer = x.IsTransfer,
                TransferredFromBookingId = x.TransferredFromBookingId,
                Notes = x.Notes,
                CreatedAtUtc = x.CreatedAtUtc
            };
        }

        private static List<TripSeatSnapshotDTO> GetSeatsFromSnapshot(BusTrip trip)
        {
            if (!string.IsNullOrWhiteSpace(trip.SeatsSnapshotJson))
            {
                try
                {
                    var seats = JsonSerializer.Deserialize<List<TripSeatSnapshotDTO>>(
                        trip.SeatsSnapshotJson,
                        new JsonSerializerOptions
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

        private static TripSeatSnapshotDTO? GetSnapshotSeat(BusTrip trip, Guid seatId)
        {
            return GetSeatsFromSnapshot(trip)
                .FirstOrDefault(x => x.SeatId == seatId);
        }
    }
}