using BusinessLayer.DTOs.Account;
using BusinessLayer.Models;
using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Functions
{
    public interface IAdminManager
    {
        Task<ResponceApi<List<AccountsDTO>>> GetAllUsersAsync(Guid currentUserId);
        Task<ResponceApi<string>> CreateUserAsync(CreateNewAccountDTO model);
        Task<ResponceApi<string>> ChangeBlockStatusAsync(Guid userId, bool Dev = false);
        Task<ResponceApi<string>> ChangeActivateStatusAsync(Guid userId, bool Dev = false);
        Task<ResponceApi<string>> ChangePasswordAsync(Guid userId, string newPassword, bool Dev = false);
    }

    public class AdminManager : IAdminManager
    {
        private readonly DBContext _db;
        private readonly IDataHasher _hasher;
        private readonly IDataCiphers _cipher;
        private readonly CairoTimeService _timeService;

        public AdminManager(DBContext db, IDataCiphers cipher, IDataHasher hasher, CairoTimeService timeService)
        {
            _db = db;
            _cipher = cipher;
            _hasher = hasher;
            _timeService = timeService;
        }

        public async Task<ResponceApi<List<AccountsDTO>>> GetAllUsersAsync(Guid currentUserId)
        {
            try
            {
                var firstAdmin = await _db.Users
                    .Where(u => u.Type == _hasher.HashComparison(AccountType.Admin.ToString()))
                    .OrderBy(u => u.JoinDate)
                    .Select(u => u.UserId)
                    .FirstOrDefaultAsync();
                
                var users = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.UserId != firstAdmin && u.UserId != currentUserId)
                    .OrderByDescending(u => u.JoinDate)
                    .Select(u => new AccountsDTO
                    {
                        Id = u.UserId,
                        FullName = u.FullName,
                        UserName = u.UserName,
                        PhoneNumber = u.PhoneNumber,
                        NationalId = u.NationalId,
                        Role = _cipher.Decrypt(u.TypeChipher),
                        IsBlocked = u.Blocked,
                        Activate = u.Activate,
                        JoinDate = _timeService.UtcToCairo(u.JoinDate),
                        LastLogin = u.LastLogin.HasValue
                            ? _timeService.UtcToCairo(u.LastLogin.Value)
                            : null
                    })
                    .ToListAsync();

                return ResponceApi<List<AccountsDTO>>.Ok(users, "Users retrieved successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<List<AccountsDTO>>.Fail(
                    "Failed to retrieve users.",
                    ex.Message
                );
            }
        }

        public async Task<ResponceApi<string>> CreateUserAsync(CreateNewAccountDTO model)
        {
            try
            {
                model.UserName = model.UserName.Trim().ToLowerInvariant();
                model.PhoneNumber = model.PhoneNumber.Trim();
                model.NationalId = model.NationalId.Trim();

                var errors = new List<string>();

                if (await _db.Users.AnyAsync(u => u.UserName == model.UserName))
                    errors.Add("Username is already used.");

                if (await _db.Users.AnyAsync(u => u.PhoneNumber == model.PhoneNumber))
                    errors.Add("Phone number is already used.");

                if (await _db.Users.AnyAsync(u => u.NationalId == model.NationalId))
                    errors.Add("National ID is already used.");

                //if (model.AccountType == AccountType.Admin)
                //    errors.Add("Admin is already created.");

                if (model.Password != model.ConfirmPassword)
                    errors.Add("Password confirmation does not match.");

                if (errors.Any())
                    return ResponceApi<string>.Fail("Invalid user data.", errors.ToArray());

                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    FullName = model.FullName.Trim(),
                    UserName = model.UserName,
                    PhoneNumber = model.PhoneNumber,
                    NationalId = model.NationalId,
                    PasswordHash = _hasher.HashData(model.Password),
                    Type = _hasher.HashComparison(model.AccountType.ToString()),
                    TypeChipher = _cipher.Encrypt(model.AccountType.ToString()),
                    JoinDate = DateTime.UtcNow,
                    Activate = true,
                    Login = false,
                    Blocked = false,
                    FailedLoginAttempts = 0
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                return ResponceApi<string>.Ok(user.UserId.ToString(), "User created successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<string>.Fail("Failed to create user.", ex.Message);
            }
        }

        public async Task<ResponceApi<string>> ChangeActivateStatusAsync(Guid userId, bool Dev = false)
        {
            try
            {
                if (!Dev)
                {
                    var firstAdmin = await _db.Users
                        .Where(u => u.Type == _hasher.HashComparison(AccountType.Admin.ToString()))
                        .OrderBy(u => u.JoinDate)
                        .Select(u => u.UserId)
                        .FirstOrDefaultAsync();

                    if (firstAdmin == userId)
                        return ResponceApi<string>.Fail("Cannot change status of the first admin.", "Invalid user id.");
                }
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return ResponceApi<string>.Fail("User not found.", "Invalid user id.");

                user.Activate = !user.Activate;
                await _db.SaveChangesAsync();

                var message = user.Activate
                    ? "User activated successfully."
                    : "User deactivated successfully.";

                return ResponceApi<string>.Ok(user.UserId.ToString(), message);
            }
            catch (Exception ex)
            {
                return ResponceApi<string>.Fail("Failed to change user status.", ex.Message);
            }
        }

        public async Task<ResponceApi<string>> ChangeBlockStatusAsync(Guid userId, bool Dev = false)
        {
            try
            {
                if (!Dev)
                {
                    var firstAdmin = await _db.Users
                        .Where(u => u.Type == _hasher.HashComparison(AccountType.Admin.ToString()))
                        .OrderBy(u => u.JoinDate)
                        .Select(u => u.UserId)
                        .FirstOrDefaultAsync();

                    if (firstAdmin == userId)
                        return ResponceApi<string>.Fail("Cannot change status of the first admin.", "Invalid user id.");
                }

                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return ResponceApi<string>.Fail("User not found.", "Invalid user id.");

                user.Blocked = !user.Blocked;
                await _db.SaveChangesAsync();

                var message = user.Blocked
                    ? "User blocked successfully."
                    : "User unblocked successfully.";

                return ResponceApi<string>.Ok(user.UserId.ToString(), message);
            }
            catch (Exception ex)
            {
                return ResponceApi<string>.Fail("Failed to change user status.", ex.Message);
            }
        }

        public async Task<ResponceApi<string>> ChangePasswordAsync(Guid userId, string newPassword, bool Dev = false)
        {
            try
            {
                if (!Dev)
                {
                    var firstAdmin = await _db.Users
                        .Where(u => u.Type == _hasher.HashComparison(AccountType.Admin.ToString()))
                        .OrderBy(u => u.JoinDate)
                        .Select(u => u.UserId)
                        .FirstOrDefaultAsync();

                    if (firstAdmin == userId)
                        return ResponceApi<string>.Fail("Cannot change status of the first admin.", "Invalid user id.");
                }

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                    return ResponceApi<string>.Fail("Invalid password.", "Password must be at least 6 characters.");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return ResponceApi<string>.Fail("User not found.", "Invalid user id.");

                user.PasswordHash = _hasher.HashData(newPassword);
                await _db.SaveChangesAsync();

                return ResponceApi<string>.Ok(user.UserId.ToString(), "Password changed successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<string>.Fail("Failed to change password.", ex.Message);
            }
        }
    }
}