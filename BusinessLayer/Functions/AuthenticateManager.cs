using BusinessLayer.DTOs.Account;
using BusinessLayer.DTOs.Auth;
using BusinessLayer.Models;
using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Functions
{
    public interface IAuthenticateManager
    {
        Task<ResponceApi<string>> CreateNewAccount(CreateNewAccountDTO createAccount);
        Task<ResponceApi<LoginResponceDTO>> Login(LoginDTO login);
        Task<ResponceApi<string>> ChangePassword(ChangePasswordDTO changePassword);
        Task<ResponceApi<string>> ChangeFields(ChangeFieldsDTO changeFields);
        Task<ResponceApi<MyAccountDTO>> GetMyAccount(Guid userId);
        Task<ResponceApi<string>> ChangeActivateStatus(Guid userId, bool activate);
        Task<ResponceApi<List<AccountsDTO>>> GetAllAccounts();
        Task<ResponceApi<string>> Logout(Guid userId, Guid sessionId);
        int GetOtpExpireMinutes();
        Task<ResponceApi<string>> SendTelegramOtp();
        Task<ResponceApi<string>> VerifyTelegramOtp(string inputCode);
        Task<ResponceApi<LoginResponceDTO>> LoginAsUser(Guid userId);
    }

    public class AuthenticateManager : IAuthenticateManager
    {
        private readonly DBContext _db;
        private readonly IDataHasher _dataHasher;
        private readonly IDataCiphers _dataCiphers;
        private readonly CairoTimeService _cairoTimeService;
        private readonly TelegramSettings _telegramSettings;
        private readonly ITelegramService _telegramService;
        private readonly ITokenSessionService _tokenSessionService;
        private readonly bool BlockAdminOnFailedLogin = true;
        private readonly int FailedLoginAttemptsCount = 5;

        public AuthenticateManager(DBContext data, IDataHasher dataHasher, IDataCiphers dataCiphers, CairoTimeService cairoTimeService, ITokenSessionService tokenSessionService, IOptions<TelegramSettings> telegramSettings, ITelegramService telegramService)
        {
            _db = data;
            _dataHasher = dataHasher;
            _dataCiphers = dataCiphers;
            _cairoTimeService = cairoTimeService;
            _tokenSessionService = tokenSessionService;
            _telegramSettings = telegramSettings.Value;
            _telegramService = telegramService;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////Users Functions//////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////

        public async Task<ResponceApi<string>> CreateNewAccount(CreateNewAccountDTO createAccount)
        {
            var responce = new ResponceApi<string>();

            try
            {
                // Normalize
                createAccount.PhoneNumber = createAccount.PhoneNumber.Trim();
                createAccount.UserName = createAccount.UserName.Trim().ToLower();
                createAccount.NationalId = createAccount.NationalId.Trim();

                // Extra manual validations
                if (createAccount.Password != createAccount.ConfirmPassword)
                {
                    responce.Success = false;
                    responce.Message = "Password confirmation does not match.";
                    responce.Errors = new List<string> { "ConfirmPassword does not match Password." };
                    return responce;
                }

                if (_db.Users.AsNoTracking().Any(u => u.PhoneNumber == createAccount.PhoneNumber))
                {
                    responce.Success = false;
                    responce.Message = "This phone number is already registered.";
                    responce.Errors = new List<string> { "Duplicate phone number." };
                    return responce;
                }

                if (_db.Users.AsNoTracking().Any(u => u.NationalId == createAccount.NationalId))
                {
                    responce.Success = false;
                    responce.Message = "This National ID is already registered.";
                    responce.Errors = new List<string> { "Duplicate National ID." };
                    return responce;
                }

                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    UserName = createAccount.UserName,
                    FullName = createAccount.FullName,
                    PhoneNumber = createAccount.PhoneNumber,
                    PasswordHash = _dataHasher.HashData(createAccount.Password),
                    NationalId = createAccount.NationalId,
                    Type = _dataHasher.HashComparison(createAccount.AccountType.ToString()),
                    TypeChipher = _dataCiphers.Encrypt(createAccount.AccountType.ToString()),
                    JoinDate = DateTime.UtcNow,
                    Activate = true,
                    Login = false,
                    Blocked = false,
                    FailedLoginAttempts = 0,
                };

                _db.Users.Add(user);
                _db.SaveChanges();

                responce.Success = true;
                responce.Message = "Account created successfully.";
                responce.Data = user.UserId.ToString();
                return responce;
            }
            catch (Exception ex)
            {
                responce.Success = false;
                responce.Message = "Failed to create account.";
                responce.Errors = new List<string> { ex.Message };
                return responce;
            }
        }

        public async Task<ResponceApi<LoginResponceDTO>> Login(LoginDTO login)
        {
            var responce = new ResponceApi<LoginResponceDTO>();

            try
            {
                if (login == null)
                {
                    responce.Success = false;
                    responce.Message = "Login request is invalid.";
                    responce.Errors = new List<string> { "Request body is null." };
                    return responce;
                }

                if (string.IsNullOrWhiteSpace(login.EmailOrPhoneOrUsernameOrNationalId) ||
                    string.IsNullOrWhiteSpace(login.Password))
                {
                    responce.Success = false;
                    responce.Message = "Invalid email or password.";
                    responce.Errors = new List<string> { "Login credentials are required." };
                    return responce;
                }

                // Normalize input
                login.EmailOrPhoneOrUsernameOrNationalId = login.EmailOrPhoneOrUsernameOrNationalId.Trim().ToLower();

                var hashInput = _dataHasher.HashComparison(login.EmailOrPhoneOrUsernameOrNationalId);

                var userByPhone = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == login.EmailOrPhoneOrUsernameOrNationalId);
                var userByUsername = await _db.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == login.EmailOrPhoneOrUsernameOrNationalId);
                var userByNationalId = await _db.Users.FirstOrDefaultAsync(u => u.NationalId == login.EmailOrPhoneOrUsernameOrNationalId);

                var user = userByPhone ?? userByUsername ?? userByNationalId;
                if (user == null)
                {
                    responce.Success = false;
                    responce.Message = "Invalid email or password.";
                    responce.Errors = new List<string> { "User not found." };
                    return responce;
                }

                if (!user.Activate || (user.Blocked && (_dataCiphers.Decrypt(user.TypeChipher) == AccountType.Admin.ToString() && BlockAdminOnFailedLogin)))
                {
                    responce.Success = false;
                    responce.Message = "This account is blocked.";
                    responce.Errors = new List<string> { "Blocked account." };
                    return responce;
                }

                var checkPassword = _dataHasher.VerifyHashed(login.Password, user.PasswordHash);
                if (!checkPassword)
                {
                    user.FailedLoginAttempts++;

                    if (user.FailedLoginAttempts >= FailedLoginAttemptsCount && (_dataCiphers.Decrypt(user.TypeChipher) == AccountType.Admin.ToString() && BlockAdminOnFailedLogin))
                        user.Blocked = true;

                    await _db.SaveChangesAsync();

                    responce.Success = false;
                    responce.Message = $"Invalid email or password.it will block after {Math.Max(0, FailedLoginAttemptsCount - user.FailedLoginAttempts)} times";
                    responce.Errors = new List<string> { "Wrong password." };
                    return responce;
                }

                // Reset failed attempts before creating session
                user.FailedLoginAttempts = 0;
                user.Blocked = false;
                user.Login = true;
                user.LastLogin = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                // Create session + access token + refresh token
                var sessionResult = await _tokenSessionService.CreateSessionAsync(user.UserId);

                if (!sessionResult.Success || sessionResult.Data == null)
                {
                    responce.Success = false;
                    responce.Message = "Login failed.";
                    responce.Errors = new List<string> { "Unable to create session." };
                    return responce;
                }

                responce.Success = true;
                responce.Message = "Login successful.";
                responce.Data = new LoginResponceDTO
                {
                    Token = sessionResult.Data.AccessToken,
                    ExpireAt = _cairoTimeService.UtcToCairo(sessionResult.Data.AccessTokenExpiresAt),
                    UserID = user.UserId,
                    RefreshToken = sessionResult.Data.RefreshToken,
                    RefreshTokenExpireAt = _cairoTimeService.UtcToCairo(sessionResult.Data.RefreshTokenExpiresAt),
                    SessionId = sessionResult.Data.SessionId
                };

                return responce;
            }
            catch (Exception ex)
            {
                responce.Success = false;
                responce.Message = "Login failed.";
                responce.Errors = new List<string> { ex.Message };
                return responce;
            }
        }

        public async Task<ResponceApi<string>> ChangePassword(ChangePasswordDTO changePassword)
        {
            var responce = new ResponceApi<string>();

            User user = _db.Users.FirstOrDefault(u => u.UserId == changePassword.UserId)!;

            if (user == null)
            {
                responce.Success = false;
                responce.Message = "Invalid Account.";
                responce.Errors = new List<string> { "User not found." };
                return responce;
            }
            if (!_dataHasher.VerifyHashed(changePassword.OldPassword, user.PasswordHash))
            {
                responce.Success = false;
                responce.Message = "Invalid Old Password.";
                responce.Errors = new List<string> { "Old Password was wrong." };
                return responce;
            }
            if (changePassword.NewPassword != changePassword.ConfirmPassword)
            {
                responce.Success = false;
                responce.Message = "Password confirmation does not match.";
                responce.Errors = new List<string> { "ConfirmPassword does not match Password." };
                return responce;
            }

            user.PasswordHash = _dataHasher.HashData(changePassword.NewPassword);
            _db.SaveChanges();
            responce.Success = true;
            responce.Message = "Password Change Successfully.";
            responce.Data = user.UserId.ToString();

            return responce;
        }

        public async Task<ResponceApi<string>> ChangeFields(ChangeFieldsDTO changeFields)
        {
            var responce = new ResponceApi<string>();
            User user = _db.Users.FirstOrDefault(u => u.UserId == changeFields.UserId)!;

            if (user == null)
            {
                responce.Success = false;
                responce.Message = "Invalid Account.";
                responce.Errors = new List<string> { "User not found." };
                return responce;
            }

            if (_db.Users.FirstOrDefault(u => u.UserId != changeFields.UserId && u.PhoneNumber == changeFields.PhoneNumber) != null)
            {
                responce.Success = false;
                responce.Message = "Phone number already in use.";
                responce.Errors = new List<string> { "Phone number is already associated with another account." };
                return responce;
            }

            if (_db.Users.FirstOrDefault(u => u.UserId != changeFields.UserId && u.NationalId == changeFields.NationalId) != null)
            {
                responce.Success = false;
                responce.Message = "National ID already in use.";
                responce.Errors = new List<string> { "National ID is already associated with another account." };
                return responce;
            }

            user.PhoneNumber = changeFields.PhoneNumber == string.Empty ? user.PhoneNumber : changeFields.PhoneNumber;
            user.NationalId = changeFields.NationalId == string.Empty ? user.NationalId : changeFields.NationalId;
            user.FullName = changeFields.FullName == string.Empty ? user.FullName : changeFields.FullName;

            _db.SaveChanges();
            responce.Success = true;
            responce.Message = "Fields updated successfully.";
            responce.Data = user.UserId.ToString();
            return responce;
        }

        public async Task<ResponceApi<MyAccountDTO>> GetMyAccount(Guid userId)
        {
            var responce = new ResponceApi<MyAccountDTO>();
            User user = _db.Users.FirstOrDefault(u => u.UserId == userId)!;

            if (user == null)
            {
                responce.Success = false;
                responce.Message = "Invalid Account.";
                responce.Data = null;
                responce.Errors = new List<string> { "User not found." };
                return responce;
            }
            responce.Success = true;
            responce.Message = "Get Data User successfully.";
            responce.Data = new MyAccountDTO
            {
                UserId = userId,
                UserName = user.UserName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                NationalId = user.NationalId,
                AccountType = Enum.Parse<AccountType>(_dataCiphers.Decrypt(user.TypeChipher)),
                JoinDate = _cairoTimeService.UtcToCairo(user.JoinDate),
                LastLogin = _cairoTimeService.UtcToCairo(user.LastLogin!.Value),
                Activate = user.Activate,
                Login = user.Login,
            };
            return responce;
        }

        public async Task<ResponceApi<string>> ChangeActivateStatus(Guid userId, bool activate)
        {
            var responce = new ResponceApi<string>();
            User user = _db.Users.FirstOrDefault(u => u.UserId == userId)!;
            if (user == null)
            {
                responce.Success = false;
                responce.Message = "Invalid Account.";
                responce.Errors = new List<string> { "User not found." };
                return responce;
            }
            user.Activate = activate;
            _db.SaveChanges();
            responce.Success = true;
            responce.Message = "Activate status changed successfully.";
            responce.Data = user.UserId.ToString();
            return responce;
        }

        public async Task<ResponceApi<List<AccountsDTO>>> GetAllAccounts()
        {
            var responce = new ResponceApi<List<AccountsDTO>>();
            try
            {
                var users = await _db.Users.Select(u => new AccountsDTO
                {
                    Id = u.UserId,
                    UserName = u.UserName,
                    FullName = u.FullName,
                    Role = _dataCiphers.Decrypt(u.TypeChipher),
                    Activate = u.Activate,
                    LastLogin = u.LastLogin,
                    JoinDate = u.JoinDate,
                    IsBlocked = u.Blocked
                }).ToListAsync();

                responce.Success = true;
                responce.Message = "Accounts retrieved successfully.";
                responce.Data = users;
                return responce;
            }
            catch (Exception ex)
            {
                responce.Success = false;
                responce.Message = "Failed to retrieve accounts.";
                responce.Errors = new List<string> { ex.Message };
                return responce;
            }
        }

        public async Task<ResponceApi<string>> Logout(Guid userId, Guid sessionId)
        {
            var responce = new ResponceApi<string>();

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    responce.Success = false;
                    responce.Message = "Invalid Account.";
                    responce.Errors = new List<string> { "User not found." };
                    return responce;
                }

                var revokeSessionResult = await _tokenSessionService.RevokeSessionAsync(sessionId);

                if (!revokeSessionResult.Success)
                {
                    responce.Success = false;
                    responce.Message = revokeSessionResult.Message ?? "Logout failed.";
                    responce.Errors = revokeSessionResult.Errors ?? new List<string> { "Unable to revoke session." };
                    return responce;
                }

                var hasAnotherActiveSession = await _db.UserSessions
                    .AnyAsync(s => s.UserId == userId && s.IsActive);

                user.Login = hasAnotherActiveSession;

                await _db.SaveChangesAsync();

                responce.Success = true;
                responce.Message = "Logout successful.";
                responce.Data = user.UserId.ToString();
                return responce;
            }
            catch (Exception ex)
            {
                responce.Success = false;
                responce.Message = "Logout failed.";
                responce.Errors = new List<string> { ex.Message };
                return responce;
            }
        }

        public int GetOtpExpireMinutes()
        {
            return _telegramSettings.OtpExpireMinutes;
        }
        
        public async Task<ResponceApi<string>> SendTelegramOtp()
        {
            var response = new ResponceApi<string>();

            try
            {
                var settings = await _db.TelegramSettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    settings = new TelegramSettings
                    {
                        BotToken = _telegramSettings.BotToken,
                        OtpExpireMinutes = _telegramSettings.OtpExpireMinutes,
                        ChatId = _telegramSettings.ChatId,
                        TimeErrorOTP = 0,
                        CreateAt = DateTime.UtcNow,
                        LastSendOTP = DateTime.MinValue
                    };

                    _db.TelegramSettings.Add(settings);
                    await _db.SaveChangesAsync();
                }

                var elapsed = DateTime.UtcNow - settings.LastSendOTP;
                var waitTime = TimeSpan.FromMinutes(_telegramSettings.SendOTPAfterMinuts);

                if (elapsed < waitTime)
                {
                    var remaining = waitTime - elapsed;

                    return ResponceApi<string>.Fail(
                        $"Please wait {Math.Ceiling(remaining.TotalMinutes)} minute(s) before requesting a new OTP.",
                        "OTP request too soon."
                    );
                }

                //if (settings.TimeErrorOTP >= _telegramSettings.TimeErrorOTP)
                //{
                //    return ResponceApi<string>.Fail(
                //        "Too many OTP requests. Please try again later.",
                //        "OTP limit exceeded."
                //    );
                //}

                string code = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1000000).ToString("D6");

                var message = $"AM Travel password reset code: {code}\n" +
                              $"This code expires in {_telegramSettings.OtpExpireMinutes} minutes.";

                bool sent = await _telegramService.SendMessageAsync(_telegramSettings.ChatId, message);

                if (!sent)
                {
                    return ResponceApi<string>.Fail(
                        "Failed to send OTP on Telegram.",
                        "Telegram send failed."
                    );
                }

                settings.Code = code;
                settings.TimeErrorOTP++;
                settings.LastSendOTP = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return ResponceApi<string>.Ok("OTP sent to Telegram.");
            }
            catch (Exception ex)
            {
                return ResponceApi<string>.Fail(
                    "Something went wrong while sending OTP.",
                    ex.Message
                );
            }
        }

        public async Task<ResponceApi<string>> VerifyTelegramOtp(string inputCode)
        {
            var response = new ResponceApi<string>();

            try
            {
                var settings = await _db.TelegramSettings.FirstOrDefaultAsync();

                if (settings == null || string.IsNullOrWhiteSpace(settings.Code))
                {
                    return ResponceApi<string>.Fail("No OTP found. Please request a new one.", "OTP not generated.");
                }

                var expireTime = settings.LastSendOTP.AddMinutes(_telegramSettings.OtpExpireMinutes);

                if (DateTime.UtcNow > expireTime)
                {
                    return ResponceApi<string>.Fail("OTP has expired. Please request a new one.", "OTP expired.");
                }

                //if (settings.TimeErrorOTP >= _telegramSettings.TimeErrorOTP)
                //{
                //    return ResponceApi<string>.Fail("Too many OTP requests. Please try again later.", "OTP limit exceeded.");
                //}

                if (settings.Code != inputCode)
                {
                    settings.TimeErrorOTP++;

                    await _db.SaveChangesAsync();

                    return ResponceApi<string>.Fail("Invalid OTP.", "Wrong code.");
                }

                settings.Code = string.Empty;
                settings.TimeErrorOTP = 0;

                await _db.SaveChangesAsync();

                return ResponceApi<string>.Ok("OTP verified successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<string>.Fail("Something went wrong while verifying OTP.", ex.Message);
            }
        }

        public async Task<ResponceApi<LoginResponceDTO>> LoginAsUser(Guid userId)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return ResponceApi<LoginResponceDTO>.Fail("User not found.", "Invalid user id.");

                if (!user.Activate || user.Blocked)
                    return ResponceApi<LoginResponceDTO>.Fail("User is inactive or blocked.", "Cannot login as this user.");

                var sessionResult = await _tokenSessionService.CreateSessionAsync(user.UserId);

                if (!sessionResult.Success || sessionResult.Data == null)
                    return ResponceApi<LoginResponceDTO>.Fail("Login failed.", "Unable to create session.");

                user.Login = true;
                user.LastLogin = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return ResponceApi<LoginResponceDTO>.Ok(new LoginResponceDTO
                {
                    Token = sessionResult.Data.AccessToken,
                    ExpireAt = _cairoTimeService.UtcToCairo(sessionResult.Data.AccessTokenExpiresAt),
                    UserID = user.UserId,
                    RefreshToken = sessionResult.Data.RefreshToken,
                    RefreshTokenExpireAt = _cairoTimeService.UtcToCairo(sessionResult.Data.RefreshTokenExpiresAt),
                    SessionId = sessionResult.Data.SessionId
                }, "Logged in successfully.");
            }
            catch (Exception ex)
            {
                return ResponceApi<LoginResponceDTO>.Fail("Developer login failed.", ex.Message);
            }
        }
    }
}