using BusinessLayer.DTOs;
using BusinessLayer.DTOs.Book;
using BusinessLayer.DTOs.Bus;
using BusinessLayer.DTOs.Company;
using BusinessLayer.DTOs.Trip;
using BusinessLayer.Models;
using DataLayer.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Functions
{
    public interface ITransportationManager
    {
        Task<ResponceApi<List<CompanyDTO>>> GetCompanies();
        Task<ResponceApi<Guid>> CreateCompany(CreateCompanyDTO dto);
        Task<ResponceApi<UpdateCompanyDTO>> GetCompanyForEdit(Guid companyId);
        Task<ResponceApi<bool>> ChangeCompanyStatus(Guid companyId);
        Task<ResponceApi<bool>> UpdateCompany(UpdateCompanyDTO dto);
        Task<ResponceApi<bool>> IsPhoneUsedByAnotherCompany(Guid companyId, string phoneNumber);
        Task<ResponceApi<bool>> DeleteCompany(Guid companyId);

        //-----------------------------------------------------------------//
        
        Task<ResponceApi<List<BusDTO>>> GetBuses();
        Task<ResponceApi<Guid>> CreateBus(CreateBusDTO dto);
        Task<ResponceApi<BusDetailsDTO>> GetBusDetails(Guid busId);
        Task<ResponceApi<UpdateBusDTO>> GetBusForEdit(Guid busId);
        Task<ResponceApi<bool>> UpdateBus(UpdateBusDTO dto);
        Task<ResponceApi<bool>> ChangeBusStatus(Guid busId);
        Task<ResponceApi<bool>> ChangeSeatStatus(Guid seatId);
        Task<ResponceApi<bool>> UpdateBusLayoutStatus(Guid busId, string layoutJson);
        Task<ResponceApi<bool>> DeleteBus(Guid busId);
        Task<ResponceApi<bool>> IsBusNameUsedByAnotherBus(Guid busId, string name);
        Task<ResponceApi<bool>> IsPlateNumberUsedByAnotherBus(Guid busId, string? plateNumber);

        //-----------------------------------------------------------------//

        Task<ResponceApi<GetTripDTO>> GetTrips(Guid? companyId, DateTime? date, TransportationTripType tripType, TripSearchDTO? search = null);
        Task<ResponceApi<List<TripSeatStatusDTO>>> GetTripSeats(Guid tripId);
        Task<ResponceApi<CompanySeatBookingDetailsDTO>> CompanySeatBookingDetailsAsync(Guid bookingId);
        Task<ResponceApi<bool>> ChangeTripSeatStatus(Guid tripId, Guid seatId);

        //-----------------------------------------------------------------//

        Task<ResponceApi<List<AvailableTripListDTO>>> GetAvailableTripsAsync(DateTime date, TransportationTripType tripType, Guid? companyId = null);
        Task<ResponceApi<string>> ConnectTripByCompanyAsync(ConnectCompanyWithTripsDTO dto);
        Task<ResponceApi<string>> DeleteConnectTripByCompanyAsync(Guid companyId, Guid tripId, bool isAdmin);
        Task<ResponceApi<List<AvailableTripListDTO>>> GetTripsConnectedByCompanyAsync(Guid companyId, DateTime date);

        //-----------------------------------------------------------------//

        Task<ResponceApi<List<AvailableTripListDTO>>> GetCompanyTripAccountingAsync(TripSearchDTO search);
        Task<ResponceApi<GetTripDTO>> GetCompanyTripAccountingByCoumpanyAsync(TripSearchDTO search);
        Task<ResponceApi<string>> UpdateCompanyTripPriceAsync(Guid companyId, Guid tripId, decimal price);
        Task<ResponceApi<string>> AddCompanyTripPaymentAsync(Guid companyId, Guid tripId, decimal amount);
    }

    public class TransportationManager : ITransportationManager
    {
        private readonly DBContext _db;

        public TransportationManager(DBContext db)
        {
            _db = db;
        }

        private static List<BusLayoutItemDTO> SafeDeserializeLayout(string? json)
        {
            var result = new List<BusLayoutItemDTO>();

            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                    return result;

                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var type = SeatType.Empty;
                    var row = 0;
                    var column = 0;
                    string? label = null;
                    var isActive = true;
                    var hasDoor = false;

                    if ((item.TryGetProperty("type", out var typeElement) ||
                         item.TryGetProperty("Type", out typeElement)) &&
                        typeElement.ValueKind == System.Text.Json.JsonValueKind.Number &&
                        typeElement.TryGetInt32(out var typeValue))
                    {
                        type = ResolveSeatType(typeValue);
                    }

                    if ((item.TryGetProperty("row", out var rowElement) ||
                         item.TryGetProperty("Row", out rowElement)) &&
                        rowElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        rowElement.TryGetInt32(out row);
                    }

                    if ((item.TryGetProperty("column", out var columnElement) ||
                         item.TryGetProperty("Column", out columnElement)) &&
                        columnElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                    {
                        columnElement.TryGetInt32(out column);
                    }

                    if ((item.TryGetProperty("label", out var labelElement) ||
                         item.TryGetProperty("Label", out labelElement)) &&
                        labelElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        label = labelElement.GetString();
                    }

                    if ((item.TryGetProperty("isActive", out var activeElement) ||
                         item.TryGetProperty("IsActive", out activeElement)) &&
                        (activeElement.ValueKind == System.Text.Json.JsonValueKind.True ||
                         activeElement.ValueKind == System.Text.Json.JsonValueKind.False))
                    {
                        isActive = activeElement.GetBoolean();
                    }

                    if ((item.TryGetProperty("hasDoor", out var doorElement) ||
                         item.TryGetProperty("HasDoor", out doorElement)) &&
                        (doorElement.ValueKind == System.Text.Json.JsonValueKind.True ||
                         doorElement.ValueKind == System.Text.Json.JsonValueKind.False))
                    {
                        hasDoor = doorElement.GetBoolean();
                    }

                    if (row <= 0 || column <= 0)
                        continue;

                    result.Add(new BusLayoutItemDTO
                    {
                        Type = (int)type,
                        Row = row,
                        Column = column,
                        Label = label,
                        IsActive = isActive,
                        HasDoor = hasDoor
                    });
                }

                return result;
            }
            catch
            {
                return new List<BusLayoutItemDTO>();
            }
        }

        private static SeatType ResolveSeatType(int type)
        {
            return Enum.IsDefined(typeof(SeatType), type) ? (SeatType)type : SeatType.Empty;
        }

        public async Task<ResponceApi<List<CompanyDTO>>> GetCompanies()
        {
            try
            {
                var companies = await _db.Companies.AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new CompanyDTO
                    {
                        CompanyId = x.CompanyId,
                        Name = x.Name,
                        PhoneNumber = x.PhoneNumber,
                        Notes = x.Notes,
                        IsActive = x.IsActive
                    })
                    .ToListAsync();

                return ResponceApi<List<CompanyDTO>>.Ok(companies, "Companies retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<CompanyDTO>>.Fail("Failed to retrieve companies.", ex.Message);
            }
        }

        public async Task<ResponceApi<Guid>> CreateCompany(CreateCompanyDTO dto)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var company = new TransportationCompany
                {
                    CompanyId = Guid.NewGuid(),
                    Name = dto.Name.Trim(),
                    PhoneNumber = dto.PhoneNumber.Trim(),
                    Notes = dto.Notes?.Trim(),
                    IsActive = true
                };

                _db.Companies.Add(company);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<Guid>.Ok(company.CompanyId, "Company created successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<Guid>.Fail("Failed to create company.", ex.Message);
            }
        }

        public async Task<ResponceApi<UpdateCompanyDTO>> GetCompanyForEdit(Guid companyId)
        {
            try
            {
                var company = await _db.Companies.AsNoTracking()
                    .Where(x => x.CompanyId == companyId)
                    .Select(x => new UpdateCompanyDTO
                    {
                        CompanyId = x.CompanyId,
                        Name = x.Name,
                        PhoneNumber = x.PhoneNumber,
                        Notes = x.Notes,
                        IsActive = x.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (company == null)
                    return ResponceApi<UpdateCompanyDTO>.Fail("Company not found.");

                return ResponceApi<UpdateCompanyDTO>.Ok(company, "Company retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<UpdateCompanyDTO>.Fail("Failed to retrieve company.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> ChangeCompanyStatus(Guid companyId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var company = await _db.Companies.FirstOrDefaultAsync(x => x.CompanyId == companyId);

                if (company == null)
                    return ResponceApi<bool>.Fail("Company not found.");

                company.IsActive = !company.IsActive;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Company status updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to update company status.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> UpdateCompany(UpdateCompanyDTO dto)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var company = await _db.Companies.FirstOrDefaultAsync(x => x.CompanyId == dto.CompanyId);

                if (company == null)
                    return ResponceApi<bool>.Fail("Company not found.");

                company.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
                company.Notes = dto.Notes?.Trim();
                company.IsActive = dto.IsActive;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Company updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to update company.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> IsPhoneUsedByAnotherCompany(Guid companyId, string phoneNumber)
        {
            try
            {
                phoneNumber = phoneNumber?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return ResponceApi<bool>.Ok(false);

                var used = await _db.Companies.AsNoTracking()
                    .AnyAsync(x => x.CompanyId != companyId && x.PhoneNumber == phoneNumber);

                return ResponceApi<bool>.Ok(used);
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail("Failed to check phone number.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> DeleteCompany(Guid companyId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var company = await _db.Companies.FirstOrDefaultAsync(x => x.CompanyId == companyId);

                if (company == null)
                    return ResponceApi<bool>.Fail("Company not found.");

                var hasRelations = await _db.CompanySeatBookings.AnyAsync(x => x.CompanyId == companyId) ||
                                   await _db.CompanySeatPayments.AnyAsync(x => x.CompanyId == companyId) ||
                                   await _db.BusTrips.AnyAsync(x => x.CompanyId == companyId);

                if (hasRelations)
                {
                    company.IsActive = false;

                    _db.Companies.Update(company);

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return ResponceApi<bool>.Ok(
                        true,
                        "Company has related data, so it was deactivated only."
                    );
                }

                _db.Companies.Remove(company);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(
                    true,
                    "Company deleted successfully."
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return ResponceApi<bool>.Fail(
                    "Failed to delete company.",
                    ex.Message
                );
            }
        }

        //------------------------------------------------------------------------------------//

        public async Task<ResponceApi<List<BusDTO>>> GetBuses()
        {
            try
            {
                var buses = await _db.Buses.AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new BusDTO
                    {
                        BusId = x.BusId,
                        Name = x.Name,
                        PlateNumber = x.PlateNumber,
                        SeatsCount = x.SeatsCount,
                        Notes = x.Notes,
                        LayoutRows = x.LayoutRows,
                        LayoutColumns = x.LayoutColumns,
                        FromLocation = x.FromLocation,
                        ToLocation = x.ToLocation,
                        LayoutJson = x.LayoutJson,
                        IsActive = x.IsActive
                    })
                    .ToListAsync();

                return ResponceApi<List<BusDTO>>.Ok(buses, "Buses retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<BusDTO>>.Fail("Failed to retrieve buses.", ex.Message);
            }
        }

        public async Task<ResponceApi<Guid>> CreateBus(CreateBusDTO dto)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var bus = new Bus
                {
                    BusId = Guid.NewGuid(),
                    Name = dto.Name.Trim(),
                    PlateNumber = string.IsNullOrWhiteSpace(dto.PlateNumber) ? null : dto.PlateNumber.Trim(),
                    Notes = dto.Notes?.Trim(),
                    LayoutRows = dto.LayoutRows,
                    LayoutColumns = dto.LayoutColumns,
                    LayoutJson = dto.LayoutJson,
                    FromLocation = dto.FromLocation,
                    ToLocation = dto.ToLocation,
                    IsActive = true
                };

                var layout = SafeDeserializeLayout(dto.LayoutJson);

                var seatItems = layout
                    .Select(x => new { Item = x, SeatType = ResolveSeatType(x.Type) })
                    .Where(x =>
                        x.SeatType == SeatType.Normal ||
                        x.SeatType == SeatType.VIP ||
                        x.SeatType == SeatType.Driver ||
                        x.SeatType == SeatType.Assistant)
                    .OrderBy(x => x.Item.Row)
                    .ThenBy(x => x.Item.Column)
                    .ToList();

                bus.SeatsCount = seatItems.Count(x => x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP);

                int seatNumber = 1;

                foreach (var row in seatItems)
                {
                    bus.Seats.Add(new BusSeat
                    {
                        SeatId = Guid.NewGuid(),
                        BusId = bus.BusId,
                        SeatNumber = seatNumber,
                        SeatLabel = string.IsNullOrWhiteSpace(row.Item.Label) ? seatNumber.ToString() : row.Item.Label,
                        RowNumber = row.Item.Row,
                        ColumnNumber = row.Item.Column,
                        SeatType = row.SeatType,
                        FromLocation = bus.FromLocation,
                        ToLocation = bus.ToLocation,
                        IsActive = row.Item.IsActive
                    });

                    seatNumber++;
                }

                var activePassengerSeats = seatItems.Count(x =>
                    x.Item.IsActive &&
                    (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP));

                bus.IsActive = activePassengerSeats > 0;

                _db.Buses.Add(bus);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<Guid>.Ok(bus.BusId, "Bus created successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<Guid>.Fail("Failed to create bus.", ex.Message);
            }
        }

        public async Task<ResponceApi<BusDetailsDTO>> GetBusDetails(Guid busId)
        {
            try
            {
                var bus = await _db.Buses.Include(x => x.Seats)
                    .AsNoTracking()
                    .Where(x => x.BusId == busId)
                    .Select(x => new BusDetailsDTO
                    {
                        BusId = x.BusId,
                        Name = x.Name,
                        PlateNumber = x.PlateNumber,
                        SeatsCount = x.SeatsCount,
                        LayoutRows = x.LayoutRows,
                        LayoutColumns = x.LayoutColumns,
                        LayoutJson = x.LayoutJson,
                        FromLocation = x.FromLocation,
                        ToLocation = x.ToLocation,
                        Notes = x.Notes,
                        IsActive = x.IsActive,
                        Seats = x.Seats.OrderBy(s => s.RowNumber).ThenBy(s => s.ColumnNumber)
                            .Select(s => new TripSeatStatusDTO
                            {
                                SeatId = s.SeatId,
                                SeatNumber = s.SeatNumber,
                                SeatLabel = s.SeatLabel,
                                SeatType = s.SeatType,
                                RowNumber = s.RowNumber,
                                ColumnNumber = s.ColumnNumber,
                                IsActive = s.IsActive,
                                IsReserved = false
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (bus == null)
                    return ResponceApi<BusDetailsDTO>.Fail("Bus not found.");

                return ResponceApi<BusDetailsDTO>.Ok(bus, "Bus retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<BusDetailsDTO>.Fail("Failed to retrieve bus.", ex.Message);
            }
        }

        public async Task<ResponceApi<UpdateBusDTO>> GetBusForEdit(Guid busId)
        {
            try
            {
                var bus = await _db.Buses.AsNoTracking()
                    .Where(x => x.BusId == busId)
                    .Select(x => new UpdateBusDTO
                    {
                        BusId = x.BusId,
                        Name = x.Name,
                        PlateNumber = x.PlateNumber,
                        LayoutRows = x.LayoutRows,
                        LayoutColumns = x.LayoutColumns,
                        LayoutJson = x.LayoutJson,
                        FromLocation = x.FromLocation,
                        ToLocation = x.ToLocation,
                        Notes = x.Notes,
                        IsActive = x.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (bus == null)
                    return ResponceApi<UpdateBusDTO>.Fail("Bus not found.");

                return ResponceApi<UpdateBusDTO>.Ok(bus, "Bus retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<UpdateBusDTO>.Fail("Failed to retrieve bus.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> IsBusNameUsedByAnotherBus(Guid busId, string name)
        {
            try
            {
                name = name?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name))
                    return ResponceApi<bool>.Ok(false);

                var used = await _db.Buses.AsNoTracking()
                    .AnyAsync(x => x.BusId != busId && x.Name.ToLower() == name.ToLower());

                return ResponceApi<bool>.Ok(used);
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail("Failed to check bus name.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> IsPlateNumberUsedByAnotherBus(Guid busId, string? plateNumber)
        {
            try
            {
                plateNumber = plateNumber?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(plateNumber))
                    return ResponceApi<bool>.Ok(false);

                var used = await _db.Buses.AsNoTracking()
                    .AnyAsync(x => x.BusId != busId &&
                                   x.PlateNumber != null &&
                                   x.PlateNumber.ToLower() == plateNumber.ToLower());

                return ResponceApi<bool>.Ok(used);
            }
            catch (Exception ex)
            {
                return ResponceApi<bool>.Fail("Failed to check plate number.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> ChangeBusStatus(Guid busId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var bus = await _db.Buses.Include(x => x.Seats).FirstOrDefaultAsync(x => x.BusId == busId);

                if (bus == null)
                    return ResponceApi<bool>.Fail("Bus not found.");

                if (bus.IsActive)
                {
                    bus.IsActive = false;
                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return ResponceApi<bool>.Ok(true, "Bus deactivated successfully.");
                }

                var hasAvailablePassengerSeats = bus.Seats.Any(x =>
                    x.IsActive &&
                    (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP));

                if (!hasAvailablePassengerSeats)
                    return ResponceApi<bool>.Fail("Cannot activate this bus because it has no active Normal or VIP seats.");

                bus.IsActive = true;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Bus activated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to update bus status.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> ChangeSeatStatus(Guid seatId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var seat = await _db.BusSeats.FirstOrDefaultAsync(x => x.SeatId == seatId);

                if (seat == null)
                    return ResponceApi<bool>.Fail("Seat not found.");

                seat.IsActive = !seat.IsActive;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Seat status updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to update seat status.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> UpdateBusLayoutStatus(Guid busId, string layoutJson)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var bus = await _db.Buses
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(x => x.BusId == busId);

                if (bus == null)
                    return ResponceApi<bool>.Fail("Bus not found.");

                var layout = SafeDeserializeLayout(layoutJson);

                // ── تحديث الـ Bus Template ──────────────────────────────────
                bus.LayoutJson = System.Text.Json.JsonSerializer.Serialize(layout);

                foreach (var seat in bus.Seats)
                {
                    var item = layout.FirstOrDefault(x =>
                        x.Row == seat.RowNumber &&
                        x.Column == seat.ColumnNumber &&
                        ResolveSeatType(x.Type) == seat.SeatType);

                    if (item != null)
                        seat.IsActive = item.IsActive;
                }

                var activePassengerSeats = bus.Seats.Count(x =>
                    x.IsActive &&
                    (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP));

                bus.IsActive = activePassengerSeats > 0;

                // ── الـ Trips القديمة لا تُمس — كل Trip محتفظة بـ SeatsSnapshotJson الخاص بها ──
                // الـ Trips الجديدة هتاخد الـ layout الجديد تلقائيًا عند إنشائها عبر CreateTrip

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Bus layout updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to update bus layout.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> UpdateBus(UpdateBusDTO dto)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var bus = await _db.Buses
                    .Include(x => x.Seats)
                    .FirstOrDefaultAsync(x => x.BusId == dto.BusId);

                if (bus == null)
                    return ResponceApi<bool>.Fail("Bus not found.");

                var layout = SafeDeserializeLayout(dto.LayoutJson);

                bus.Name = dto.Name.Trim();
                bus.PlateNumber = string.IsNullOrWhiteSpace(dto.PlateNumber) ? null : dto.PlateNumber.Trim();
                bus.LayoutRows = dto.LayoutRows;
                bus.LayoutColumns = dto.LayoutColumns;
                bus.FromLocation = dto.FromLocation;
                bus.ToLocation = dto.ToLocation;
                bus.LayoutJson = System.Text.Json.JsonSerializer.Serialize(layout);
                bus.Notes = dto.Notes?.Trim();

                var seatItems = layout
                    .Select(x => new { Item = x, SeatType = ResolveSeatType(x.Type) })
                    .Where(x =>
                        x.SeatType == SeatType.Normal ||
                        x.SeatType == SeatType.VIP ||
                        x.SeatType == SeatType.Driver ||
                        x.SeatType == SeatType.Assistant)
                    .OrderBy(x => x.Item.Row)
                    .ThenBy(x => x.Item.Column)
                    .ToList();

                bus.SeatsCount = seatItems.Count(x =>
                    x.SeatType == SeatType.Normal ||
                    x.SeatType == SeatType.VIP);

                var existingSeats = bus.Seats.ToList();

                var usedSeatIds = await _db.BookingTransportationSeats
                    .Where(x => existingSeats.Select(s => s.SeatId).Contains(x.SeatId))
                    .Select(x => x.SeatId)
                    .Distinct()
                    .ToListAsync();

                var companyUsedSeatIds = await _db.CompanySeatBookings
                    .Where(x =>
                        (x.SeatId != null && existingSeats.Select(s => s.SeatId).Contains(x.SeatId.Value)) ||
                        (x.ReturnSeatId != null && existingSeats.Select(s => s.SeatId).Contains(x.ReturnSeatId.Value)))
                    .Select(x => x.SeatId)
                    .Where(x => x != null)
                    .Select(x => x!.Value)
                    .Distinct()
                    .ToListAsync();

                usedSeatIds.AddRange(companyUsedSeatIds);
                usedSeatIds = usedSeatIds.Distinct().ToList();

                var usedSeatIdSet = usedSeatIds.ToHashSet();

                int seatNumber = 1;
                var updatedSeatIds = new HashSet<Guid>();

                foreach (var row in seatItems)
                {
                    var existingSeat = existingSeats.FirstOrDefault(x =>
                        x.RowNumber == row.Item.Row &&
                        x.ColumnNumber == row.Item.Column);

                    if (existingSeat != null)
                    {
                        existingSeat.SeatNumber = seatNumber;
                        existingSeat.SeatLabel = string.IsNullOrWhiteSpace(row.Item.Label)
                            ? seatNumber.ToString()
                            : row.Item.Label;

                        existingSeat.RowNumber = row.Item.Row;
                        existingSeat.ColumnNumber = row.Item.Column;
                        existingSeat.SeatType = row.SeatType;
                        existingSeat.FromLocation = bus.FromLocation;
                        existingSeat.ToLocation = bus.ToLocation;
                        existingSeat.IsActive = row.Item.IsActive;

                        updatedSeatIds.Add(existingSeat.SeatId);
                    }
                    else
                    {
                        var newSeat = new BusSeat
                        {
                            SeatId = Guid.NewGuid(),
                            BusId = bus.BusId,
                            SeatNumber = seatNumber,
                            SeatLabel = string.IsNullOrWhiteSpace(row.Item.Label)
                                ? seatNumber.ToString()
                                : row.Item.Label,
                            RowNumber = row.Item.Row,
                            ColumnNumber = row.Item.Column,
                            FromLocation = bus.FromLocation,
                            ToLocation = bus.ToLocation,
                            SeatType = row.SeatType,
                            IsActive = row.Item.IsActive
                        };

                        _db.BusSeats.Add(newSeat);
                        updatedSeatIds.Add(newSeat.SeatId);
                    }

                    seatNumber++;
                }

                var removedFromLayoutSeats = existingSeats
                    .Where(x => !updatedSeatIds.Contains(x.SeatId))
                    .ToList();

                foreach (var seat in removedFromLayoutSeats)
                {
                    if (usedSeatIdSet.Contains(seat.SeatId))
                    {
                        // مقعد مستخدم في حجز قديم: لا نحذفه حتى لا نكسر العلاقات
                        seat.IsActive = false;
                    }
                    else
                    {
                        // مقعد غير مستخدم نهائيًا: ممكن يتحذف بأمان
                        _db.BusSeats.Remove(seat);
                    }
                }

                var activePassengerSeats = seatItems.Count(x =>
                    x.Item.IsActive &&
                    (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP));

                bus.IsActive = dto.IsActive && activePassengerSeats > 0;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Bus updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to update bus.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> DeleteBus(Guid busId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var bus = await _db.Buses.Include(x => x.Seats).FirstOrDefaultAsync(x => x.BusId == busId);

                if (bus == null)
                    return ResponceApi<bool>.Fail("Bus not found.");

                var hasTrips = await _db.BusTrips.AnyAsync(x => x.BusId == busId);

                if (hasTrips)
                    return ResponceApi<bool>.Fail("Cannot delete this bus because it has trips.");

                _db.BusSeats.RemoveRange(bus.Seats);
                _db.Buses.Remove(bus);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Bus deleted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to delete bus.", ex.Message);
            }
        }

        //---------------------------------------------------------------------------------//

        public async Task<ResponceApi<GetTripDTO>> GetTrips(Guid? companyId, DateTime? date, TransportationTripType tripType, TripSearchDTO? search = null)
        {
            try
            {
                var selectedDate = date?.Date ?? DateTime.Today;

                search ??= new TripSearchDTO();

                var companies = await _db.Companies
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.CompanyId.ToString(),
                        Text = $"{x.Name} - {x.PhoneNumber}",
                        Selected = companyId.HasValue && x.CompanyId == companyId.Value
                    })
                    .ToListAsync();

                var query = _db.BusTrips
                    .AsNoTracking()
                    .Include(x => x.Bus)
                    .Include(x => x.Company)
                    .Include(x => x.ReservedSeats)
                    .Where(x => !x.IsClosed)
                    .AsQueryable();

                if (search.DateFrom.HasValue)
                    query = query.Where(x => x.TripDate.Date >= search.DateFrom.Value.Date);

                if (search.DateTo.HasValue)
                    query = query.Where(x => x.TripDate.Date <= search.DateTo.Value.Date);

                if (!search.DateFrom.HasValue &&
                    !search.DateTo.HasValue &&
                    date.HasValue)
                {
                    query = query.Where(x => x.TripDate.Date == selectedDate);
                }

                if (search.CompanyId.HasValue &&
                    search.CompanyId.Value != Guid.Empty)
                {
                    query = query.Where(x => x.CompanyId == search.CompanyId.Value);
                }

                if (!string.IsNullOrWhiteSpace(search.Location))
                {
                    var location = search.Location.Trim().ToLower();

                    query = query.Where(x =>
                        (x.FromLocation ?? x.Bus.FromLocation ?? "")
                            .ToLower()
                            .Contains(location)
                        ||
                        (x.ToLocation ?? x.Bus.ToLocation ?? "")
                            .ToLower()
                            .Contains(location));
                }

                if (search.TripType.HasValue)
                {
                    if (search.TripType.Value == TransportationTripType.Departure)
                    {
                        query = query.Where(x =>
                            x.Direction == TripDirection.Departure);
                    }

                    if (search.TripType.Value == TransportationTripType.Return)
                    {
                        query = query.Where(x =>
                            x.Direction == TripDirection.Return);
                    }

                    if (search.TripType.Value == TransportationTripType.RoundTrip)
                    {
                        query = query.Where(x =>
                            x.CompanyTripGroupId != null);
                    }
                }

                var trips = await query
                    .OrderBy(x => x.TripDate)
                    .ThenBy(x => x.Direction)
                    .ThenBy(x => x.FromLocation)
                    .ThenBy(x => x.ToLocation)
                    .ThenBy(x => x.BusNameSnapshot)
                    .ToListAsync();

                var tripIds = trips
                    .Select(x => x.TripId)
                    .Distinct()
                    .ToList();

                var companySeatBookings = tripIds.Any()
                    ? await _db.CompanySeatBookings
                        .AsNoTracking()
                        .Include(x => x.Company)
                        .Where(x =>
                            (
                                x.TripId != null &&
                                tripIds.Contains(x.TripId.Value) &&
                                x.SeatId != null
                            )
                            ||
                            (
                                x.ReturnTripId != null &&
                                tripIds.Contains(x.ReturnTripId.Value) &&
                                x.ReturnSeatId != null
                            ))
                        .ToListAsync()
                    : new List<CompanySeatBooking>();

                var companyBookingsByTrip =
                    new Dictionary<Guid, List<CompanySeatBooking>>();

                foreach (var item in companySeatBookings)
                {
                    if (item.TripId.HasValue &&
                        item.SeatId.HasValue &&
                        tripIds.Contains(item.TripId.Value))
                    {
                        if (!companyBookingsByTrip.ContainsKey(item.TripId.Value))
                        {
                            companyBookingsByTrip[item.TripId.Value] =
                                new List<CompanySeatBooking>();
                        }

                        companyBookingsByTrip[item.TripId.Value].Add(item);
                    }

                    if (item.ReturnTripId.HasValue &&
                        item.ReturnSeatId.HasValue &&
                        tripIds.Contains(item.ReturnTripId.Value))
                    {
                        if (!companyBookingsByTrip.ContainsKey(item.ReturnTripId.Value))
                        {
                            companyBookingsByTrip[item.ReturnTripId.Value] =
                                new List<CompanySeatBooking>();
                        }

                        companyBookingsByTrip[item.ReturnTripId.Value].Add(item);
                    }
                }

                var accountingTrips = trips
                    .Select(x =>
                    {
                        companyBookingsByTrip.TryGetValue(
                            x.TripId,
                            out var companyBookings);

                        companyBookings ??= new List<CompanySeatBooking>();

                        var clientReserved = x.ReservedSeats
                            .Select(r => r.SeatId)
                            .Distinct()
                            .Count();

                        var companyReserved = companyBookings
                            .Select(c =>
                                c.TripId == x.TripId
                                    ? c.SeatId
                                    : c.ReturnTripId == x.TripId
                                        ? c.ReturnSeatId
                                        : null)
                            .Where(id => id.HasValue)
                            .Select(id => id!.Value)
                            .Distinct()
                            .Count();

                        var totalReserved =
                            clientReserved + companyReserved;

                        var totalSeats =
                            x.SeatsCountSnapshot > 0
                                ? x.SeatsCountSnapshot
                                : x.Bus.SeatsCount;

                        var companyName =
                            x.CompanyId.HasValue &&
                            x.Company != null
                                ? x.Company.Name
                                : "Not Linked";

                        var companyPhoneNumber =
                            x.CompanyId.HasValue &&
                            x.Company != null
                                ? x.Company.PhoneNumber
                                : string.Empty;

                        return new AvailableTripListDTO
                        {
                            TripId = x.TripId,
                            BusId = x.BusId,

                            CompanyId = x.CompanyId,
                            CompanyName = companyName,
                            CompanyPhoneNumber = companyPhoneNumber,
                            CompanyTripGroupId = x.CompanyTripGroupId,

                            BusName = !string.IsNullOrWhiteSpace(x.BusNameSnapshot)
                                ? x.BusNameSnapshot
                                : x.Bus.Name,

                            PlateNumber = !string.IsNullOrWhiteSpace(x.PlateNumberSnapshot)
                                ? x.PlateNumberSnapshot
                                : x.Bus.PlateNumber ?? string.Empty,

                            Direction = x.Direction,

                            DirectionText =
                                x.Direction == TripDirection.Departure
                                    ? "Go"
                                    : "Return",

                            TripDate = x.TripDate,

                            FromLocation =
                                !string.IsNullOrWhiteSpace(x.FromLocation)
                                    ? x.FromLocation
                                    : x.Bus.FromLocation ?? string.Empty,

                            ToLocation =
                                !string.IsNullOrWhiteSpace(x.ToLocation)
                                    ? x.ToLocation
                                    : x.Bus.ToLocation ?? string.Empty,

                            TotalSeats = totalSeats,

                            ReservedSeats = totalReserved,

                            AvailableSeats =
                                Math.Max(0, totalSeats - totalReserved),

                            CompanyTripPrice = x.CompanyTripPrice,

                            CompanyTripPaidAmount =
                                x.CompanyTripPaidAmount,

                            CompanyTripRemainingAmount =
                                Math.Max(
                                    0,
                                    x.CompanyTripPrice -
                                    x.CompanyTripPaidAmount)
                        };
                    })
                    .Where(x => x.ReservedSeats > 0)
                    .ToList();

                var locations = accountingTrips
                    .SelectMany(x => new[]
                    {
                x.FromLocation,
                x.ToLocation
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .Select(x => new SelectListItem
                    {
                        Value = x,
                        Text = x
                    })
                    .ToList();

                return ResponceApi<GetTripDTO>.Ok(new GetTripDTO
                {
                    CompanyId = companyId,
                    TripDate = selectedDate,
                    TripType = tripType,

                    Companies = companies,

                    LinkedTrips = companyId.HasValue
                        ? accountingTrips
                            .Where(x => x.CompanyId == companyId)
                            .ToList()
                        : new List<AvailableTripListDTO>(),

                    AccountingTrips = accountingTrips,

                    Locations = locations,

                    Search = search
                });
            }
            catch (Exception ex)
            {
                return ResponceApi<GetTripDTO>.Fail(
                    "Failed to load company trips page.",
                    ex.Message);
            }
        }

        public async Task<ResponceApi<List<TripSeatStatusDTO>>> GetTripSeats(Guid tripId)
        {
            try
            {
                var trip = await _db.BusTrips
                    .AsNoTracking()
                    .Include(x => x.Bus)
                        .ThenInclude(x => x.Seats)
                    .Include(x => x.ReservedSeats)
                        .ThenInclude(x => x.Booking)
                            .ThenInclude(x => x.PhoneNumbers)
                    .FirstOrDefaultAsync(x => x.TripId == tripId);

                if (trip == null)
                    return ResponceApi<List<TripSeatStatusDTO>>.Fail("Trip not found.");

                var generalFrom = !string.IsNullOrWhiteSpace(trip.FromLocation)
                    ? trip.FromLocation
                    : trip.Bus.FromLocation;

                var generalTo = !string.IsNullOrWhiteSpace(trip.ToLocation)
                    ? trip.ToLocation
                    : trip.Bus.ToLocation;

                var generalTripRouteText = trip.Direction == TripDirection.Departure
                    ? "Go"
                    : "Return";

                var clientReservedBySeat = trip.ReservedSeats
                    .Where(x => x.Booking != null && !x.Booking.IsDeleted)
                    .GroupBy(x => x.SeatId)
                    .ToDictionary(x => x.Key, x => x.First());

                var companyReservations = await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Include(x => x.Company)
                    .Where(x =>
                        (x.TripId == tripId && x.SeatId != null) ||
                        (x.ReturnTripId == tripId && x.ReturnSeatId != null))
                    .ToListAsync();

                var companyReservedBySeat = new Dictionary<Guid, CompanySeatBooking>();

                foreach (var item in companyReservations)
                {
                    if (item.TripId == tripId && item.SeatId.HasValue)
                        companyReservedBySeat[item.SeatId.Value] = item;

                    if (item.ReturnTripId == tripId && item.ReturnSeatId.HasValue)
                        companyReservedBySeat[item.ReturnSeatId.Value] = item;
                }

                List<TripSeatSnapshotDTO> snapshot;

                if (!string.IsNullOrWhiteSpace(trip.SeatsSnapshotJson))
                {
                    snapshot = System.Text.Json.JsonSerializer.Deserialize<List<TripSeatSnapshotDTO>>(
                        trip.SeatsSnapshotJson,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }) ?? new List<TripSeatSnapshotDTO>();
                }
                else
                {
                    snapshot = trip.Bus.Seats
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
                }

                var seats = snapshot
                    .OrderBy(x => x.RowNumber)
                    .ThenBy(x => x.ColumnNumber)
                    .Select(x =>
                    {
                        clientReservedBySeat.TryGetValue(x.SeatId, out var clientReserved);
                        companyReservedBySeat.TryGetValue(x.SeatId, out var companyReserved);

                        var booking = clientReserved?.Booking;

                        var isReservedByClient = clientReserved != null;
                        var isReservedByCompany = companyReserved != null;

                        var isReturnCompanySeat =
                            companyReserved != null &&
                            companyReserved.ReturnTripId == tripId &&
                            companyReserved.ReturnSeatId == x.SeatId;

                        var companySeatLabel = isReturnCompanySeat
                            ? companyReserved?.ReturnSeatLabelSnapshot
                            : companyReserved?.SeatLabelSnapshot;

                        var companySeatNumber = isReturnCompanySeat
                            ? companyReserved?.ReturnSeatNumberSnapshot
                            : companyReserved?.SeatNumberSnapshot;

                        var companyClientName = !string.IsNullOrWhiteSpace(companyReserved?.ClientName)
                            ? companyReserved.ClientName
                            : companyReserved?.Company?.Name;

                        var companyCode = companyReserved != null
                            ? $"Company-{companyReserved.BookingId.ToString()[..8]}"
                            : null;

                        var seatRouteFrom = generalFrom;
                        var seatRouteTo = generalTo;

                        var tripRouteText = generalTripRouteText;
                        var tripTypeText = generalTripRouteText;

                        if (companyReserved != null)
                        {
                            tripTypeText = companyReserved.ClientTripType.ToString();

                            if (isReturnCompanySeat)
                            {
                                tripRouteText = "Return";

                                seatRouteFrom = !string.IsNullOrWhiteSpace(companyReserved.ToLocation)
                                    ? companyReserved.ToLocation
                                    : generalFrom;

                                seatRouteTo = !string.IsNullOrWhiteSpace(companyReserved.FromLocation)
                                    ? companyReserved.FromLocation
                                    : generalTo;
                            }
                            else
                            {
                                tripRouteText = "Go";

                                seatRouteFrom = !string.IsNullOrWhiteSpace(companyReserved.FromLocation)
                                    ? companyReserved.FromLocation
                                    : generalFrom;

                                seatRouteTo = !string.IsNullOrWhiteSpace(companyReserved.ToLocation)
                                    ? companyReserved.ToLocation
                                    : generalTo;
                            }
                        }

                        var phoneNumbersText = string.Empty;

                        if (isReservedByClient && booking != null)
                        {
                            phoneNumbersText = string.Join(" - ", booking.PhoneNumbers
                                .OrderByDescending(p => p.Prime)
                                .Select(p => p.PhoneNumber)
                                .Where(p => !string.IsNullOrWhiteSpace(p)));
                        }
                        else if (isReservedByCompany)
                        {
                            phoneNumbersText = companyReserved?.ClientPhone ?? string.Empty;
                        }

                        var companyGroupKey = companyReserved == null
                            ? null
                            : string.Join("|", new[]
                            {
                        companyReserved.CompanyId.ToString(),
                        companyReserved.TripId?.ToString() ?? "",
                        companyReserved.ReturnTripId?.ToString() ?? "",
                        companyReserved.ClientName?.Trim() ?? "",
                        companyReserved.ClientPhone?.Trim() ?? "",
                        companyReserved.ClientTripType.ToString(),
                        companyReserved.FromLocation?.Trim() ?? "",
                        companyReserved.ToLocation?.Trim() ?? "",
                        companyReserved.ReturnFromLocation?.Trim() ?? "",
                        companyReserved.ReturnToLocation?.Trim() ?? "",
                        companyReserved.PricePerSeat.ToString("0.####")
                            });

                        return new TripSeatStatusDTO
                        {
                            SeatId = x.SeatId,
                            SeatNumber = x.SeatNumber,
                            TripDate = trip.TripDate,
                            SeatLabel = !string.IsNullOrWhiteSpace(x.SeatLabel)
                                ? x.SeatLabel
                                : !string.IsNullOrWhiteSpace(companySeatLabel)
                                    ? companySeatLabel
                                    : companySeatNumber.HasValue && companySeatNumber.Value > 0
                                        ? companySeatNumber.Value.ToString()
                                        : x.SeatNumber.ToString(),

                            SeatType = x.SeatType,
                            RowNumber = x.RowNumber,
                            ColumnNumber = x.ColumnNumber,
                            IsActive = x.IsActive,

                            IsReserved = isReservedByClient || isReservedByCompany,

                            IsCompanyBooking = isReservedByCompany && !isReservedByClient,
                            CompanySeatBookingId = companyReserved?.BookingId,
                            CompanyBookingGroupKey = companyGroupKey,
                            CompanyId = companyReserved?.CompanyId,
                            CompanyName = companyReserved?.Company?.Name,

                            BookingId = booking?.BookingID,

                            ReservedByClient = isReservedByClient
                                ? booking?.ClientName
                                : companyClientName,

                            BookingCode = isReservedByClient
                                ? booking?.Code
                                : companyCode,

                            HotelTotal = booking?.HotelTotal ?? 0,

                            TransportationTotal = isReservedByClient
                                ? booking?.TransportationTotal ?? 0
                                : companyReserved?.PricePerSeat ?? 0,

                            Discount = booking?.Discount ?? 0,

                            GrandTotal = isReservedByClient
                                ? booking?.GrandTotal ?? 0
                                : companyReserved?.PricePerSeat ?? 0,

                            PaidAmount = booking?.PaidAmount ?? 0,

                            RemainingAmount = isReservedByClient
                                ? booking?.RemainingAmount ?? 0
                                : companyReserved?.PricePerSeat ?? 0,

                            HasHotel = booking?.HasHotel ?? false,

                            HasTransportation = isReservedByClient
                                ? booking?.HasTransportation ?? false
                                : isReservedByCompany,

                            PhoneNumbersText = phoneNumbersText,

                            TripTypeText = tripTypeText,
                            TripRouteText = tripRouteText,

                            SeatRouteFrom = seatRouteFrom,
                            SeatRouteTo = seatRouteTo,

                            GeneralRouteFrom = generalFrom,
                            GeneralRouteTo = generalTo,
                            HotelName = booking?.HotelName,
                            BusName = trip.BusNameSnapshot ?? trip.Bus.Name,
                            PlateNumber = trip.PlateNumberSnapshot ?? trip.Bus.PlateNumber
                            
                        };
                    })
                    .ToList();

                return ResponceApi<List<TripSeatStatusDTO>>.Ok(seats, "Trip seats retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<TripSeatStatusDTO>>.Fail("Failed to retrieve trip seats.", ex.Message);
            }
        }

        public async Task<ResponceApi<CompanySeatBookingDetailsDTO>> CompanySeatBookingDetailsAsync(Guid bookingId)
        {
            try
            {
                if (bookingId == Guid.Empty)
                    return ResponceApi<CompanySeatBookingDetailsDTO>.Fail("Booking id is required.");

                var booking = await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Include(x => x.Company)
                    .Include(x => x.Trip)
                        .ThenInclude(x => x!.Bus)
                    .Include(x => x.ReturnTrip)
                        .ThenInclude(x => x!.Bus)
                    .Include(x => x.Seat)
                    .Include(x => x.ReturnSeat)
                    .FirstOrDefaultAsync(x => x.BookingId == bookingId);

                if (booking == null)
                    return ResponceApi<CompanySeatBookingDetailsDTO>.Fail("Company seat booking not found.");

                var tripFrom = booking.Trip?.FromLocation ?? booking.Trip?.Bus?.FromLocation ?? booking.FromLocation;
                var tripTo = booking.Trip?.ToLocation ?? booking.Trip?.Bus?.ToLocation ?? booking.ToLocation;

                var returnFrom = booking.ReturnTrip?.FromLocation ?? booking.ReturnTrip?.Bus?.FromLocation ?? booking.ReturnFromLocation;
                var returnTo = booking.ReturnTrip?.ToLocation ?? booking.ReturnTrip?.Bus?.ToLocation ?? booking.ReturnToLocation;

                var seatLabel = !string.IsNullOrWhiteSpace(booking.SeatLabelSnapshot)
                    ? booking.SeatLabelSnapshot
                    : booking.SeatNumberSnapshot > 0
                        ? booking.SeatNumberSnapshot.ToString()
                        : booking.Seat?.SeatLabel ?? booking.Seat?.SeatNumber.ToString() ?? string.Empty;

                var returnSeatLabel = !string.IsNullOrWhiteSpace(booking.ReturnSeatLabelSnapshot)
                    ? booking.ReturnSeatLabelSnapshot
                    : booking.ReturnSeatNumberSnapshot > 0
                        ? booking.ReturnSeatNumberSnapshot.ToString()
                        : booking.ReturnSeat?.SeatLabel ?? booking.ReturnSeat?.SeatNumber.ToString() ?? string.Empty;

                var dto = new CompanySeatBookingDetailsDTO
                {
                    BookingId = booking.BookingId,

                    CompanyId = booking.CompanyId,
                    CompanyName = booking.Company?.Name ?? string.Empty,
                    CompanyPhoneNumber = booking.Company?.PhoneNumber ?? string.Empty,

                    ClientName = booking.ClientName ?? string.Empty,
                    ClientPhone = booking.ClientPhone ?? string.Empty,

                    ClientTripType = booking.ClientTripType,
                    BookingDirection = booking.BookingDirection,

                    TripId = booking.TripId,
                    TripDate = booking.TripDate ?? booking.Trip?.TripDate,
                    TripRoute = $"{tripFrom} → {tripTo}",
                    TripBusName = !string.IsNullOrWhiteSpace(booking.Trip?.BusNameSnapshot)
                        ? booking.Trip.BusNameSnapshot
                        : booking.Trip?.Bus?.Name ?? "External Bus",
                    TripPlateNumber = !string.IsNullOrWhiteSpace(booking.Trip?.PlateNumberSnapshot)
                        ? booking.Trip.PlateNumberSnapshot
                        : booking.Trip?.Bus?.PlateNumber ?? string.Empty,
                    SeatLabel = seatLabel,

                    ReturnTripId = booking.ReturnTripId,
                    ReturnTripDate = booking.ReturnTripDate ?? booking.ReturnTrip?.TripDate,
                    ReturnTripRoute = string.IsNullOrWhiteSpace(returnFrom) && string.IsNullOrWhiteSpace(returnTo)
                        ? string.Empty
                        : $"{returnFrom} → {returnTo}",
                    ReturnBusName = !string.IsNullOrWhiteSpace(booking.ReturnTrip?.BusNameSnapshot)
                        ? booking.ReturnTrip.BusNameSnapshot
                        : booking.ReturnTrip?.Bus?.Name ?? string.Empty,
                    ReturnPlateNumber = !string.IsNullOrWhiteSpace(booking.ReturnTrip?.PlateNumberSnapshot)
                        ? booking.ReturnTrip.PlateNumberSnapshot
                        : booking.ReturnTrip?.Bus?.PlateNumber ?? string.Empty,
                    ReturnSeatLabel = returnSeatLabel,

                    FromLocation = booking.FromLocation ?? string.Empty,
                    ToLocation = booking.ToLocation ?? string.Empty,
                    ReturnFromLocation = booking.ReturnFromLocation ?? string.Empty,
                    ReturnToLocation = booking.ReturnToLocation ?? string.Empty,

                    PricePerSeat = booking.PricePerSeat,
                    TotalPrice = booking.TotalPrice,

                    IsRoundTrip = booking.IsRoundTrip,
                    IsTransfer = booking.IsTransfer,
                    TransferredFromBookingId = booking.TransferredFromBookingId,
                    Notes = booking.Notes ?? string.Empty,

                    CreatedAtUtc = booking.CreatedAtUtc
                };

                return ResponceApi<CompanySeatBookingDetailsDTO>.Ok(dto);
            }
            catch (Exception ex)
            {
                return ResponceApi<CompanySeatBookingDetailsDTO>.Fail(
                    "Failed to load company seat booking details.",
                    ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> ChangeTripSeatStatus(Guid tripId, Guid seatId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var trip = await _db.BusTrips.FirstOrDefaultAsync(x => x.TripId == tripId && !x.IsClosed);

                if (trip == null)
                    return ResponceApi<bool>.Fail("Trip not found or closed.");

                if (string.IsNullOrWhiteSpace(trip.SeatsSnapshotJson))
                    return ResponceApi<bool>.Fail("This trip has no seats snapshot.");

                var seats = System.Text.Json.JsonSerializer.Deserialize<List<TripSeatSnapshotDTO>>(
                    trip.SeatsSnapshotJson,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<TripSeatSnapshotDTO>();

                var seat = seats.FirstOrDefault(x => x.SeatId == seatId);

                if (seat == null)
                    return ResponceApi<bool>.Fail("Seat not found in trip snapshot.");

                var allowedTypes = new[]
                {
                    SeatType.Normal,
                    SeatType.VIP,
                    SeatType.Bathroom
                };

                if (!allowedTypes.Contains(seat.SeatType))
                {
                    return ResponceApi<bool>.Fail(
                        "Only Normal, VIP, and Bathroom seats can be activated/deactivated. Driver and Assistant seats cannot be changed.");
                }

                var isReservedByClient = await _db.BookingTransportationSeats
                    .AnyAsync(x => x.TripId == tripId && x.SeatId == seatId);

                var isReservedByCompany = await _db.CompanySeatBookings
                    .AnyAsync(x =>
                        (x.TripId == tripId && x.SeatId == seatId) ||
                        (x.ReturnTripId == tripId && x.ReturnSeatId == seatId));

                if (isReservedByClient || isReservedByCompany)
                    return ResponceApi<bool>.Fail("Cannot change status of a reserved seat.");

                seat.IsActive = !seat.IsActive;

                trip.SeatsSnapshotJson = System.Text.Json.JsonSerializer.Serialize(seats);

                trip.SeatsCountSnapshot = seats.Count(x =>
                    x.IsActive &&
                    (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP));

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Trip seat status updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to update trip seat status.", ex.Message);
            }
        }

        //---------------------------------------------------------------------------------//

        public async Task<ResponceApi<List<AvailableTripListDTO>>> GetAvailableTripsAsync(DateTime date, TransportationTripType tripType, Guid? companyId = null)
        {
            try
            {
                var selectedDate = date.Date;

                var query = _db.BusTrips
                    .AsNoTracking()
                    .Where(x =>
                        !x.IsClosed &&
                        x.TripDate.Date == selectedDate &&
                        x.CompanyId == null);

                if (tripType == TransportationTripType.Departure)
                    query = query.Where(x => x.Direction == TripDirection.Departure);

                if (tripType == TransportationTripType.Return)
                    query = query.Where(x => x.Direction == TripDirection.Return);

                if (tripType == TransportationTripType.RoundTrip)
                    query = query.Where(x =>
                        x.Direction == TripDirection.Departure ||
                        x.Direction == TripDirection.Return);

                var data = await query
                    .Select(x => new
                    {
                        Trip = x,

                        ClientReservedSeatsCount = x.ReservedSeats
                            .Count(r => r.Booking != null && !r.Booking.IsDeleted),

                        CompanyReservedSeatsCount = _db.CompanySeatBookings.Count(c =>
                            (x.Direction == TripDirection.Departure &&
                             c.TripId == x.TripId &&
                             c.SeatId != null) ||

                            (x.Direction == TripDirection.Return &&
                             c.ReturnTripId == x.TripId &&
                             c.ReturnSeatId != null))
                    })
                    .Where(x =>
                        x.ClientReservedSeatsCount > 0 ||
                        x.CompanyReservedSeatsCount > 0)
                    .OrderBy(x => x.Trip.Direction)
                    .ThenBy(x => x.Trip.FromLocation)
                    .ThenBy(x => x.Trip.ToLocation)
                    .ThenBy(x => x.Trip.BusNameSnapshot)
                    .Select(x => new AvailableTripListDTO
                    {
                        TripId = x.Trip.TripId,
                        BusId = x.Trip.BusId,
                        BusName = x.Trip.BusNameSnapshot,
                        PlateNumber = x.Trip.PlateNumberSnapshot ?? string.Empty,

                        Direction = x.Trip.Direction,
                        DirectionText = x.Trip.Direction == TripDirection.Departure
                            ? "Go"
                            : "Return",

                        TripDate = x.Trip.TripDate,
                        FromLocation = x.Trip.FromLocation ?? string.Empty,
                        ToLocation = x.Trip.ToLocation ?? string.Empty,

                        TotalSeats = x.Trip.SeatsCountSnapshot,

                        ReservedSeats =
                            x.ClientReservedSeatsCount + x.CompanyReservedSeatsCount,

                        AvailableSeats =
                            x.Trip.SeatsCountSnapshot -
                            (x.ClientReservedSeatsCount + x.CompanyReservedSeatsCount),

                        CompanyTripPrice = x.Trip.CompanyTripPrice,
                        CompanyTripPaidAmount = x.Trip.CompanyTripPaidAmount,

                        CompanyTripRemainingAmount =
                            x.Trip.CompanyTripPrice - x.Trip.CompanyTripPaidAmount < 0
                                ? 0
                                : x.Trip.CompanyTripPrice - x.Trip.CompanyTripPaidAmount
                    })
                    .ToListAsync();

                return ResponceApi<List<AvailableTripListDTO>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ResponceApi<List<AvailableTripListDTO>>.Fail("Failed to load trips.", ex.Message);
            }
        }

        public async Task<ResponceApi<string>> ConnectTripByCompanyAsync(ConnectCompanyWithTripsDTO dto)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                dto.TripDate = dto.TripDate.Date;

                var errors = ValidateDto(dto);
                if (errors.Count > 0)
                    return ResponceApi<string>.Fail("Invalid data.", errors.ToArray());

                var company = await _db.Companies
                    .FirstOrDefaultAsync(x => x.CompanyId == dto.CompanyId && x.IsActive);

                if (company == null)
                    return ResponceApi<string>.Fail("Company not found or inactive.");

                BusTrip? departureTrip = null;
                BusTrip? returnTrip = null;

                if (dto.TripType == TransportationTripType.Departure ||
                    dto.TripType == TransportationTripType.RoundTrip)
                {
                    departureTrip = await LoadTripForAttachAsync(
                        dto.DepartureTripId!.Value,
                        TripDirection.Departure,
                        dto.TripDate);

                    if (departureTrip == null)
                        return ResponceApi<string>.Fail("Departure trip not found or closed.");

                    if (departureTrip.CompanyId.HasValue)
                        return ResponceApi<string>.Fail("Departure trip is already linked to another company.");

                    if (departureTrip.ReservedSeats == null || !departureTrip.ReservedSeats.Any())
                        return ResponceApi<string>.Fail("Departure trip has no reserved seats.");
                }

                if (dto.TripType == TransportationTripType.Return ||
                    dto.TripType == TransportationTripType.RoundTrip)
                {
                    returnTrip = await LoadTripForAttachAsync(
                        dto.ReturnTripId!.Value,
                        TripDirection.Return,
                        dto.TripDate);

                    if (returnTrip == null)
                        return ResponceApi<string>.Fail("Return trip not found or closed.");

                    if (returnTrip.CompanyId.HasValue)
                        return ResponceApi<string>.Fail("Return trip is already linked to another company.");

                    if (returnTrip.ReservedSeats == null || !returnTrip.ReservedSeats.Any())
                        return ResponceApi<string>.Fail("Return trip has no reserved seats.");
                }

                if (dto.TripType == TransportationTripType.RoundTrip)
                {
                    if (!IsReverseRoute(departureTrip!, returnTrip!))
                    {
                        return ResponceApi<string>.Fail("Location is incorrect. The return route must be the opposite of the outbound route. Example: Assiut -> Hurghada and return Hurghada -> Assiut.");
                    }

                    var groupId = Guid.NewGuid();

                    departureTrip!.CompanyId = dto.CompanyId;
                    departureTrip.CompanyTripGroupId = groupId;
                    departureTrip.CompanyTripPrice = 0;
                    departureTrip.CompanyTripPaidAmount = 0;

                    returnTrip!.CompanyId = dto.CompanyId;
                    returnTrip.CompanyTripGroupId = groupId;
                    returnTrip.CompanyTripPrice = 0;
                    returnTrip.CompanyTripPaidAmount = 0;
                }
                else if (dto.TripType == TransportationTripType.Departure)
                {
                    departureTrip!.CompanyId = dto.CompanyId;
                    departureTrip.CompanyTripGroupId = null;
                    departureTrip.CompanyTripPrice = 0;
                    departureTrip.CompanyTripPaidAmount = 0;
                }
                else if (dto.TripType == TransportationTripType.Return)
                {
                    returnTrip!.CompanyId = dto.CompanyId;
                    returnTrip.CompanyTripGroupId = null;
                    returnTrip.CompanyTripPrice = 0;
                    returnTrip.CompanyTripPaidAmount = 0;
                }
                else
                {
                    return ResponceApi<string>.Fail("Invalid trip type.");
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<string>.Ok(null, "Trips attached to company successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<string>.Fail("Failed to attach trips to company.", ex.Message);
            }
        }

        public async Task<ResponceApi<string>> DeleteConnectTripByCompanyAsync(Guid companyId, Guid tripId, bool isAdmin)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (companyId == Guid.Empty)
                    return ResponceApi<string>.Fail("Company is required.");

                if (tripId == Guid.Empty)
                    return ResponceApi<string>.Fail("Trip is required.");

                var trip = await _db.BusTrips
                    .FirstOrDefaultAsync(x => x.TripId == tripId && x.CompanyId == companyId);

                if (trip == null)
                    return ResponceApi<string>.Fail("Trip not found for this company.");

                List<BusTrip> tripsToDetach;

                if (trip.CompanyTripGroupId.HasValue)
                {
                    var groupId = trip.CompanyTripGroupId.Value;

                    tripsToDetach = await _db.BusTrips
                        .Where(x =>
                            x.CompanyId == companyId &&
                            x.CompanyTripGroupId == groupId)
                        .ToListAsync();
                }
                else
                    tripsToDetach = new List<BusTrip> { trip };

                if (tripsToDetach.Any(x => x.CompanyTripPaidAmount > 0))
                    return ResponceApi<string>.Fail("Cancellation is not possible because there is an amount paid on this plane or on its flight.");

                var hasOldTrip = tripsToDetach.Any(x => x.TripDate.Date <= DateTime.Today.AddDays(-7));

                if (hasOldTrip && !isAdmin)
                    return ResponceApi<string>.Fail("Cannot delete the connection after a week from the trip date except by the admin.");

                foreach (var item in tripsToDetach)
                {
                    item.CompanyId = null;
                    item.CompanyTripGroupId = null;
                    item.CompanyTripPrice = 0;
                    item.CompanyTripPaidAmount = 0;
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var message = tripsToDetach.Count > 1
                    ? "Successfully deleted the connection of the departure and return trips from the company."
                    : "Successfully deleted the connection of the trip from the company.";

                return ResponceApi<string>.Ok(null, message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<string>.Fail("Failed to delete company trip.", ex.Message);
            }
        }

        public async Task<ResponceApi<List<AvailableTripListDTO>>> GetTripsConnectedByCompanyAsync(Guid companyId, DateTime date)
        {
            try
            {
                if (companyId == Guid.Empty)
                    return ResponceApi<List<AvailableTripListDTO>>.Fail("Company is required.");

                var selectedDate = date.Date;

                var trips = await _db.BusTrips
                    .AsNoTracking()
                    .Include(x => x.Bus)
                    .Where(x =>
                        !x.IsClosed &&
                        x.CompanyId == companyId &&
                        x.TripDate.Date == selectedDate)
                    .Select(x => new
                    {
                        x.TripId,
                        x.BusId,
                        x.CompanyTripGroupId,

                        BusName = !string.IsNullOrWhiteSpace(x.BusNameSnapshot)
                            ? x.BusNameSnapshot
                            : x.Bus != null
                                ? x.Bus.Name
                                : string.Empty,

                        PlateNumber = !string.IsNullOrWhiteSpace(x.PlateNumberSnapshot)
                            ? x.PlateNumberSnapshot
                            : x.Bus != null
                                ? x.Bus.PlateNumber ?? string.Empty
                                : string.Empty,

                        x.Direction,
                        x.TripDate,
                        x.FromLocation,
                        x.ToLocation,
                        x.SeatsCountSnapshot,
                        x.CompanyTripPrice,
                        x.CompanyTripPaidAmount,

                        ClientReservedSeatsCount = x.ReservedSeats
                            .Count(r => r.Booking != null && !r.Booking.IsDeleted)
                    })
                    .OrderBy(x => x.CompanyTripGroupId == null ? 1 : 0)
                    .ThenBy(x => x.CompanyTripGroupId)
                    .ThenBy(x => x.Direction)
                    .ThenBy(x => x.FromLocation)
                    .ThenBy(x => x.ToLocation)
                    .ThenBy(x => x.BusName)
                    .ToListAsync();

                if (!trips.Any())
                    return ResponceApi<List<AvailableTripListDTO>>.Ok(new List<AvailableTripListDTO>());

                var tripIds = trips.Select(x => x.TripId).ToList();

                var companySeatsRaw = await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Where(x =>
                        (x.TripId.HasValue &&
                         tripIds.Contains(x.TripId.Value) &&
                         x.SeatId.HasValue)
                        ||
                        (x.ReturnTripId.HasValue &&
                         tripIds.Contains(x.ReturnTripId.Value) &&
                         x.ReturnSeatId.HasValue))
                    .Select(x => new
                    {
                        x.TripId,
                        x.SeatId,
                        x.ReturnTripId,
                        x.ReturnSeatId
                    })
                    .ToListAsync();

                var companySeatsCountByTrip = companySeatsRaw
                    .SelectMany(x => new[]
                    {
                x.TripId.HasValue && x.SeatId.HasValue
                    ? new { TripId = x.TripId.Value, SeatId = x.SeatId.Value }
                    : null,

                x.ReturnTripId.HasValue && x.ReturnSeatId.HasValue
                    ? new { TripId = x.ReturnTripId.Value, SeatId = x.ReturnSeatId.Value }
                    : null
                    })
                    .Where(x => x != null)
                    .GroupBy(x => x!.TripId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x!.SeatId).Distinct().Count());

                var data = trips.Select(x =>
                {
                    var companyReservedSeatsCount =
                        companySeatsCountByTrip.TryGetValue(x.TripId, out var count)
                            ? count
                            : 0;

                    var reservedSeats = x.ClientReservedSeatsCount + companyReservedSeatsCount;
                    var availableSeats = x.SeatsCountSnapshot - reservedSeats;

                    if (availableSeats < 0)
                        availableSeats = 0;

                    return new AvailableTripListDTO
                    {
                        TripId = x.TripId,
                        BusId = x.BusId,
                        CompanyTripGroupId = x.CompanyTripGroupId,

                        BusName = x.BusName,
                        PlateNumber = x.PlateNumber,

                        Direction = x.Direction,
                        DirectionText = x.Direction == TripDirection.Departure ? "Go" : "Return",

                        TripDate = x.TripDate,

                        FromLocation = x.FromLocation ?? string.Empty,
                        ToLocation = x.ToLocation ?? string.Empty,

                        TotalSeats = x.SeatsCountSnapshot,
                        ReservedSeats = reservedSeats,
                        AvailableSeats = availableSeats,

                        CompanyTripPrice = x.CompanyTripPrice,
                        CompanyTripPaidAmount = x.CompanyTripPaidAmount,
                        CompanyTripRemainingAmount =
                            x.CompanyTripPrice - x.CompanyTripPaidAmount < 0
                                ? 0
                                : x.CompanyTripPrice - x.CompanyTripPaidAmount
                    };
                }).ToList();

                return ResponceApi<List<AvailableTripListDTO>>.Ok(data);
            }
            catch (Exception ex)
            {
                return ResponceApi<List<AvailableTripListDTO>>.Fail("Failed to load daily trips.", ex.Message);
            }
        }

        //---------------------------------------------------------------------------------//

        public async Task<ResponceApi<List<AvailableTripListDTO>>> GetCompanyTripAccountingAsync(TripSearchDTO search)
        {
            try
            {
                search ??= new TripSearchDTO();

                var query = _db.BusTrips
                    .AsNoTracking()
                    .Include(x => x.Company)
                    .Include(x => x.Bus)
                    .Include(x => x.ReservedSeats)
                    .Where(x => x.CompanyId != null);

                if (search.DateFrom.HasValue)
                    query = query.Where(x => x.TripDate.Date >= search.DateFrom.Value.Date);

                if (search.DateTo.HasValue)
                    query = query.Where(x => x.TripDate.Date <= search.DateTo.Value.Date);

                if (search.CompanyId.HasValue && search.CompanyId.Value != Guid.Empty)
                    query = query.Where(x => x.CompanyId == search.CompanyId.Value);

                if (!string.IsNullOrWhiteSpace(search.Location))
                {
                    var location = search.Location.Trim().ToLower();

                    query = query.Where(x =>
                        (x.FromLocation ?? "").ToLower().Contains(location) ||
                        (x.ToLocation ?? "").ToLower().Contains(location));
                }

                if (search.TripType.HasValue)
                {
                    if (search.TripType.Value == TransportationTripType.Departure)
                        query = query.Where(x => x.Direction == TripDirection.Departure);

                    if (search.TripType.Value == TransportationTripType.Return)
                        query = query.Where(x => x.Direction == TripDirection.Return);

                    if (search.TripType.Value == TransportationTripType.RoundTrip)
                        query = query.Where(x => x.CompanyTripGroupId != null);
                }

                if (search.PaymentFilter == CompanyTripPaymentFilter.Paid)
                {
                    query = query.Where(x =>
                        x.CompanyTripPrice > 0 &&
                        x.CompanyTripPaidAmount >= x.CompanyTripPrice);
                }

                if (search.PaymentFilter == CompanyTripPaymentFilter.Unpaid)
                {
                    query = query.Where(x =>
                        x.CompanyTripPrice <= 0 ||
                        x.CompanyTripPaidAmount < x.CompanyTripPrice);
                }

                var tripsRaw = await query
                    .OrderByDescending(x => x.TripDate)
                    .ThenBy(x => x.CompanyTripGroupId == null ? 1 : 0)
                    .ThenBy(x => x.CompanyTripGroupId)
                    .ThenBy(x => x.Direction)
                    .ThenBy(x => x.FromLocation)
                    .ThenBy(x => x.ToLocation)
                    .Select(x => new
                    {
                        x.TripId,
                        x.BusId,
                        x.CompanyId,
                        CompanyName = x.Company != null ? x.Company.Name : string.Empty,
                        CompanyPhoneNumber = x.Company != null ? x.Company.PhoneNumber : string.Empty,
                        x.CompanyTripGroupId,

                        BusName = !string.IsNullOrWhiteSpace(x.BusNameSnapshot)
                            ? x.BusNameSnapshot
                            : x.Bus != null ? x.Bus.Name : string.Empty,

                        PlateNumber = !string.IsNullOrWhiteSpace(x.PlateNumberSnapshot)
                            ? x.PlateNumberSnapshot
                            : x.Bus != null ? x.Bus.PlateNumber ?? string.Empty : string.Empty,

                        x.Direction,
                        x.TripDate,
                        FromLocation = x.FromLocation ?? string.Empty,
                        ToLocation = x.ToLocation ?? string.Empty,
                        x.SeatsCountSnapshot,
                        x.CompanyTripPrice,
                        x.CompanyTripPaidAmount,

                        ClientReservedSeatsCount = x.ReservedSeats
                            .Count(r => r.Booking != null && !r.Booking.IsDeleted)
                    })
                    .ToListAsync();

                if (!tripsRaw.Any())
                    return ResponceApi<List<AvailableTripListDTO>>.Ok(new List<AvailableTripListDTO>());

                var tripIds = tripsRaw.Select(x => x.TripId).ToList();

                var companySeatsRaw = await _db.CompanySeatBookings
                    .AsNoTracking()
                    .Where(x =>
                        (x.TripId.HasValue &&
                         tripIds.Contains(x.TripId.Value) &&
                         x.SeatId.HasValue)
                        ||
                        (x.ReturnTripId.HasValue &&
                         tripIds.Contains(x.ReturnTripId.Value) &&
                         x.ReturnSeatId.HasValue))
                    .Select(x => new
                    {
                        x.TripId,
                        x.SeatId,
                        x.ReturnTripId,
                        x.ReturnSeatId
                    })
                    .ToListAsync();

                var departureCompanySeatsCountByTrip = companySeatsRaw
                    .Where(x => x.TripId.HasValue && x.SeatId.HasValue)
                    .GroupBy(x => x.TripId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.SeatId!.Value).Distinct().Count()
                    );

                var returnCompanySeatsCountByTrip = companySeatsRaw
                    .Where(x => x.ReturnTripId.HasValue && x.ReturnSeatId.HasValue)
                    .GroupBy(x => x.ReturnTripId!.Value)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.ReturnSeatId!.Value).Distinct().Count()
                    );

                var trips = tripsRaw.Select(x =>
                {
                    var companyReservedSeatsCount = x.Direction == TripDirection.Departure
                        ? departureCompanySeatsCountByTrip.TryGetValue(x.TripId, out var depCount) ? depCount : 0
                        : returnCompanySeatsCountByTrip.TryGetValue(x.TripId, out var retCount) ? retCount : 0;

                    var reservedSeats = x.ClientReservedSeatsCount + companyReservedSeatsCount;
                    var availableSeats = x.SeatsCountSnapshot - reservedSeats;

                    if (availableSeats < 0)
                        availableSeats = 0;

                    return new AvailableTripListDTO
                    {
                        TripId = x.TripId,
                        BusId = x.BusId,
                        CompanyId = x.CompanyId,

                        CompanyName = x.CompanyName,
                        CompanyPhoneNumber = x.CompanyPhoneNumber,

                        CompanyTripGroupId = x.CompanyTripGroupId,

                        BusName = x.BusName,
                        PlateNumber = x.PlateNumber,

                        Direction = x.Direction,
                        DirectionText = x.Direction == TripDirection.Departure
                            ? "Go"
                            : "Return",

                        TripDate = x.TripDate,
                        FromLocation = x.FromLocation,
                        ToLocation = x.ToLocation,

                        TotalSeats = x.SeatsCountSnapshot,
                        ReservedSeats = reservedSeats,
                        AvailableSeats = availableSeats,

                        CompanyTripPrice = x.CompanyTripPrice,
                        CompanyTripPaidAmount = x.CompanyTripPaidAmount,
                        CompanyTripRemainingAmount =
                            x.CompanyTripPrice - x.CompanyTripPaidAmount < 0
                                ? 0
                                : x.CompanyTripPrice - x.CompanyTripPaidAmount
                    };
                }).ToList();

                return ResponceApi<List<AvailableTripListDTO>>.Ok(trips);
            }
            catch (Exception ex)
            {
                return ResponceApi<List<AvailableTripListDTO>>.Fail(
                    "Failed to load company trip accounting.",
                    ex.Message
                );
            }
        }

        public async Task<ResponceApi<GetTripDTO>> GetCompanyTripAccountingByCoumpanyAsync(TripSearchDTO search)
        {
            try
            {
                search ??= new TripSearchDTO();

                var tripsResult = await GetCompanyTripAccountingAsync(search);

                var trips = tripsResult.Data ?? new List<AvailableTripListDTO>();

                var companies = await _db.Companies
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.CompanyId.ToString(),
                        Text = x.Name + " - " + x.PhoneNumber
                    })
                    .ToListAsync();

                var locations = trips
                    .SelectMany(x => new[] { x.FromLocation, x.ToLocation })
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .OrderBy(x => x)
                    .Select(x => new SelectListItem
                    {
                        Value = x,
                        Text = x
                    })
                    .ToList();

                var model = new GetTripDTO
                {
                    Search = search,
                    AccountingTrips = trips,
                    Companies = companies,
                    Locations = locations
                };

                if (!tripsResult.Success)
                    return ResponceApi<GetTripDTO>.Fail(
                        tripsResult.Message ?? "Failed to load accounting page.",
                        tripsResult.Errors?.ToArray() ?? Array.Empty<string>()
                    );

                return ResponceApi<GetTripDTO>.Ok(model, "Accounting page loaded successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<GetTripDTO>.Fail(
                    "Failed to load accounting page.",
                    ex.Message
                );
            }
        }

        public async Task<ResponceApi<string>> UpdateCompanyTripPriceAsync(Guid companyId, Guid tripId, decimal price)
        {
            try
            {
                if (companyId == Guid.Empty)
                    return ResponceApi<string>.Fail("Company is required.");

                if (tripId == Guid.Empty)
                    return ResponceApi<string>.Fail("Trip is required.");

                if (price < 0)
                    return ResponceApi<string>.Fail("Trip price cannot be negative.");

                var trip = await _db.BusTrips
                    .FirstOrDefaultAsync(x => x.TripId == tripId && x.CompanyId == companyId);

                if (trip == null)
                    return ResponceApi<string>.Fail("Trip not found for this company.");

                if (trip.CompanyTripPaidAmount > price)
                    return ResponceApi<string>.Fail("Trip price cannot be less than paid amount.");

                trip.CompanyTripPrice = price;

                await _db.SaveChangesAsync();

                return ResponceApi<string>.Ok(null, "The flight price has been successfully updated.");
            }
            catch (Exception ex)
            {
                return ResponceApi<string>.Fail("Failed to update trip price.", ex.Message);
            }
        }

        public async Task<ResponceApi<string>> AddCompanyTripPaymentAsync(Guid companyId, Guid tripId, decimal amount)
        {
            try
            {
                if (companyId == Guid.Empty)
                    return ResponceApi<string>.Fail("Company is required.");

                if (tripId == Guid.Empty)
                    return ResponceApi<string>.Fail("Trip is required.");

                if (amount <= 0)
                    return ResponceApi<string>.Fail("Payment amount must be greater than zero.");

                var trip = await _db.BusTrips
                    .FirstOrDefaultAsync(x => x.TripId == tripId && x.CompanyId == companyId);

                if (trip == null)
                    return ResponceApi<string>.Fail("Trip not found for this company.");

                if (trip.CompanyTripPrice <= 0)
                    return ResponceApi<string>.Fail("Set trip price first.");

                var remaining = Math.Max(0, trip.CompanyTripPrice - trip.CompanyTripPaidAmount);

                if (amount > remaining)
                    return ResponceApi<string>.Fail($"Payment amount cannot exceed remaining amount: {remaining:N2}");

                trip.CompanyTripPaidAmount += amount;

                await _db.SaveChangesAsync();

                return ResponceApi<string>.Ok(null, "The payment has been successfully added.");
            }
            catch (Exception ex)
            {
                return ResponceApi<string>.Fail("Failed to add trip payment.", ex.Message);
            }
        }

        //---------------------------------------------------------------------------------//

        private async Task<BusTrip?> LoadTripForAttachAsync(Guid tripId, TripDirection direction, DateTime tripDate)
        {
            return await _db.BusTrips
                .Include(x => x.Bus)
                .Include(x => x.ReservedSeats)
                .FirstOrDefaultAsync(x =>
                    x.TripId == tripId &&
                    x.Direction == direction &&
                    x.TripDate.Date == tripDate.Date &&
                    !x.IsClosed);
        }

        private static List<string> ValidateDto(ConnectCompanyWithTripsDTO dto)
        {
            var errors = new List<string>();

            if (dto.CompanyId == Guid.Empty)
                errors.Add("Company is required.");

            if (dto.TripDate == default)
                errors.Add("Trip date is required.");

            if (!Enum.IsDefined(typeof(TransportationTripType), dto.TripType))
                errors.Add("Invalid trip type.");

            var hasDeparture = dto.DepartureTripId.HasValue && dto.DepartureTripId.Value != Guid.Empty;
            var hasReturn = dto.ReturnTripId.HasValue && dto.ReturnTripId.Value != Guid.Empty;

            if (dto.TripType == TransportationTripType.Departure)
            {
                if (!hasDeparture)
                    errors.Add("Departure trip is required.");

                if (hasReturn)
                    errors.Add("Return trip must not be sent when trip type is Departure only.");
            }

            if (dto.TripType == TransportationTripType.Return)
            {
                if (!hasReturn)
                    errors.Add("Return trip is required.");

                if (hasDeparture)
                    errors.Add("Departure trip must not be sent when trip type is Return only.");
            }

            if (dto.TripType == TransportationTripType.RoundTrip)
            {
                if (!hasDeparture)
                    errors.Add("Departure trip is required.");

                if (!hasReturn)
                    errors.Add("Return trip is required.");

                if (hasDeparture && hasReturn && dto.DepartureTripId == dto.ReturnTripId)
                    errors.Add("Cannot use the same Trip ID for departure and return.");
            }

            return errors.Distinct().ToList();
        }

        private static bool IsReverseRoute(BusTrip departureTrip, BusTrip returnTrip)
        {
            var depFrom = NormalizeLocation(departureTrip.FromLocation);
            var depTo = NormalizeLocation(departureTrip.ToLocation);
            var retFrom = NormalizeLocation(returnTrip.FromLocation);
            var retTo = NormalizeLocation(returnTrip.ToLocation);

            return depFrom == retTo && depTo == retFrom;
        }

        private static string NormalizeLocation(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
        }

    }
}