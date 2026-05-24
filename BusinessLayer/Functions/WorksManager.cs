using BusinessLayer.DTOs.Work;
using BusinessLayer.Models;
using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Functions
{
    public interface IWorksManager
    {
        Task<ResponceApi<List<WorkListItemDTO>>> GetAllWorks(Guid userId, string role, string? search, DateTime? date, ClientType? clientType);
        Task<ResponceApi<List<WorkListItemDTO>>> GetWorksToday(Guid userId, string role, string? search, ClientType? clientType);

        Task<ResponceApi<List<WorkReportUserDTO>>> GetReportUsers();
        Task<ResponceApi<List<WorkListItemDTO>>> GetReportWorks(Guid? selectedUserId, string? search, DateTime? date, ClientType? clientType);

        Task<ResponceApi<UpdateWorkDTO>> GetUpdateWorks(Guid workId, Guid userId);
        Task<ResponceApi<bool>> CreateNewWorks(CreateWorkDTO dto, Guid userId);
        Task<ResponceApi<bool>> UpdateWorks(UpdateWorkDTO dto, Guid userId);
        Task<ResponceApi<bool>> DeleteWorks(Guid workId, Guid userId);
    }

    public class WorksManager : IWorksManager
    {
        private readonly DBContext _db;
        private readonly IDataCiphers _ciphers;

        public WorksManager(DBContext db, IDataCiphers cipher)
        {
            _db = db;
            _ciphers = cipher;
        }

        public async Task<ResponceApi<List<WorkListItemDTO>>> GetAllWorks(Guid userId, string role, string? search, DateTime? date, ClientType? clientType)
        {
            try
            {
                if (userId == Guid.Empty)
                    return ResponceApi<List<WorkListItemDTO>>.Fail("Invalid user.");

                var query = _db.Works
                    .Include(x => x.User)
                    .AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .AsQueryable();

                query = ApplyWorkFilters(query, search, date, clientType);

                var data = await ProjectWorks(query)
                    .OrderByDescending(x => x.DayCreated)
                    .ToListAsync();

                return ResponceApi<List<WorkListItemDTO>>.Ok(data, "Works retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<WorkListItemDTO>>.Fail("Failed to retrieve works.", ex.Message);
            }
        }

        public async Task<ResponceApi<List<WorkListItemDTO>>> GetWorksToday(Guid userId, string role, string? search, ClientType? clientType)
        {
            try
            {
                if (userId == Guid.Empty)
                    return ResponceApi<List<WorkListItemDTO>>.Fail("Invalid user.");

                var today = DateTime.Today;

                var query = _db.Works
                    .Include(x => x.User)
                    .AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .Where(x => x.DayCreated.Date == today || x.DayUpdated.Date == today)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var value = search.Trim().ToLower();

                    query = query.Where(x =>
                        x.PhoneNumber.ToLower().Contains(value) ||
                        x.NameClient.ToLower().Contains(value));
                }

                if (clientType.HasValue)
                    query = query.Where(x => x.ClientType == clientType.Value);

                var data = await ProjectWorks(query)
                    .OrderByDescending(x => x.DayUpdated)
                    .ToListAsync();

                return ResponceApi<List<WorkListItemDTO>>.Ok(data, "Today works retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<WorkListItemDTO>>.Fail("Failed to retrieve today works.", ex.Message);
            }
        }

        public async Task<ResponceApi<List<WorkReportUserDTO>>> GetReportUsers()
        {
            try
            {
                var data = await _db.Users
                    .AsNoTracking()
                    .Where(x => x.Activate)
                    .OrderBy(x => x.FullName)
                    .Select(x => new WorkReportUserDTO
                    {
                        UserId = x.UserId,
                        UserName = x.UserName,
                        FullName = x.FullName,
                        AccountType = _ciphers.Decrypt(x.TypeChipher.ToString())
                    })
                    .ToListAsync();

                return ResponceApi<List<WorkReportUserDTO>>.Ok(data, "Report users retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<WorkReportUserDTO>>.Fail("Failed to retrieve report users.", ex.Message);
            }
        }

        public async Task<ResponceApi<List<WorkListItemDTO>>> GetReportWorks(Guid? selectedUserId, string? search, DateTime? date, ClientType? clientType)
        {
            try
            {
                var query = _db.Works
                    .Include(x => x.User)
                    .AsNoTracking()
                    .AsQueryable();

                if (selectedUserId.HasValue && selectedUserId.Value != Guid.Empty)
                    query = query.Where(x => x.UserId == selectedUserId.Value);

                query = ApplyWorkFilters(query, search, date, clientType);

                var data = await ProjectWorks(query)
                    .OrderByDescending(x => x.DayCreated)
                    .ToListAsync();

                return ResponceApi<List<WorkListItemDTO>>.Ok(data, "Report works retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<WorkListItemDTO>>.Fail("Failed to retrieve report works.", ex.Message);
            }
        }

        public async Task<ResponceApi<UpdateWorkDTO>> GetUpdateWorks(Guid workId, Guid userId)
        {
            try
            {
                if (workId == Guid.Empty || userId == Guid.Empty)
                    return ResponceApi<UpdateWorkDTO>.Fail("Invalid request.");

                var data = await _db.Works
                    .AsNoTracking()
                    .Where(x => x.WorkId == workId && x.UserId == userId)
                    .Select(x => new UpdateWorkDTO
                    {
                        WorkId = x.WorkId,
                        PhoneNumber = x.PhoneNumber,
                        NameClient = x.NameClient,
                        ClientType = x.ClientType,
                        Notes = x.Notes,
                        DayUpdated = x.DayUpdated
                    })
                    .FirstOrDefaultAsync();

                if (data == null)
                    return ResponceApi<UpdateWorkDTO>.Fail("Work not found or you do not have permission.");

                return ResponceApi<UpdateWorkDTO>.Ok(data, "Work retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<UpdateWorkDTO>.Fail("Failed to retrieve work.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> CreateNewWorks(CreateWorkDTO dto, Guid userId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (userId == Guid.Empty)
                    return ResponceApi<bool>.Fail("Invalid user.");

                var work = new Works
                {
                    WorkId = Guid.NewGuid(),
                    UserId = userId,
                    PhoneNumber = dto.PhoneNumber.Trim(),
                    NameClient = dto.NameClient.Trim(),
                    ClientType = dto.ClientType,
                    Notes = dto.Notes?.Trim() ?? string.Empty,
                    DayCreated = DateTime.Now,
                    DayUpdated = dto.DayUpdated.Date
                };

                _db.Works.Add(work);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Work created successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to create work.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> UpdateWorks(UpdateWorkDTO dto, Guid userId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (userId == Guid.Empty)
                    return ResponceApi<bool>.Fail("Invalid user.");

                var work = await _db.Works
                    .FirstOrDefaultAsync(x => x.WorkId == dto.WorkId && x.UserId == userId);

                if (work == null)
                    return ResponceApi<bool>.Fail("Work not found or you do not have permission.");

                if(dto.DayUpdated != work.DayUpdated && dto.DayUpdated < DateTime.Today)
                    return ResponceApi<bool>.Fail("Next action date cannot be in the past.");

                work.PhoneNumber = dto.PhoneNumber.Trim();
                work.NameClient = dto.NameClient.Trim();
                work.ClientType = dto.ClientType;
                work.Notes = dto.Notes?.Trim() ?? string.Empty;
                work.DayUpdated = dto.DayUpdated.Date;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Work updated successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to update work.", ex.Message);
            }
        }

        public async Task<ResponceApi<bool>> DeleteWorks(Guid workId, Guid userId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (workId == Guid.Empty || userId == Guid.Empty)
                    return ResponceApi<bool>.Fail("Invalid request.");

                var work = await _db.Works
                    .FirstOrDefaultAsync(x => x.WorkId == workId && x.UserId == userId);

                if (work == null)
                    return ResponceApi<bool>.Fail("Work not found or you do not have permission.");

                _db.Works.Remove(work);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ResponceApi<bool>.Ok(true, "Work deleted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ResponceApi<bool>.Fail("Failed to delete work.", ex.Message);
            }
        }

        private static IQueryable<Works> ApplyWorkFilters(IQueryable<Works> query, string? search, DateTime? date, ClientType? clientType)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                var value = search.Trim().ToLower();

                query = query.Where(x =>
                    x.PhoneNumber.ToLower().Contains(value) ||
                    x.NameClient.ToLower().Contains(value));
            }

            if (date.HasValue)
            {
                var selectedDate = date.Value.Date;

                query = query.Where(x =>
                    x.DayCreated.Date == selectedDate ||
                    x.DayUpdated.Date == selectedDate);
            }

            if (clientType.HasValue)
                query = query.Where(x => x.ClientType == clientType.Value);

            return query;
        }

        private static IQueryable<WorkListItemDTO> ProjectWorks(IQueryable<Works> query)
        {
            return query.Select(x => new WorkListItemDTO
            {
                WorkId = x.WorkId,
                UserId = x.UserId,
                UserName = x.User != null ? x.User.UserName : null,
                PhoneNumber = x.PhoneNumber,
                NameClient = x.NameClient,
                ClientType = x.ClientType,
                Notes = x.Notes,
                DayCreated = x.DayCreated,
                DayUpdated = x.DayUpdated
            });
        }
    }
}