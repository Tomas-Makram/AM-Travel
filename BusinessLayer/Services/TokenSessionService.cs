using BusinessLayer.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using BusinessLayer.DTOs.Auth;

namespace BusinessLayer.Services
{
    public interface ITokenSessionService
    {
        Task<ResponceApi<RefreshTokenResponseDTO>> CreateSessionAsync(Guid userId, string? ipAddress = null, string? deviceName = null);
        Task<ResponceApi<RefreshTokenResponseDTO>> RefreshSessionAsync(RefreshTokenRequestDTO dto);
        Task<ResponceApi<bool>> RevokeSessionAsync(Guid sessionId);
        Task<ResponceApi<bool>> RevokeAllUserSessionsAsync(Guid userId);
        Task TouchSessionActivityAsync(Guid sessionId);
    }

    public class TokenSessionService : ITokenSessionService
    {
        private readonly DBContext _db;
        private readonly IDataHasher _dataHasher;
        private readonly IDataCiphers _dataCiphers;
        private readonly JwtSettings _jwtSettings;
        private readonly SessionSettings _sessionSettings;

        public TokenSessionService(DBContext db, IDataHasher dataHasher, IDataCiphers dataCiphers, IOptions<JwtSettings> jwtOptions, IOptions<SessionSettings> sessionOptions)
        {
            _db = db;
            _dataHasher = dataHasher;
            _dataCiphers = dataCiphers;
            _jwtSettings = jwtOptions.Value;
            _sessionSettings = sessionOptions.Value;
        }

        public async Task<ResponceApi<RefreshTokenResponseDTO>> CreateSessionAsync(Guid userId, string? ipAddress = null, string? deviceName = null)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return ResponceApi<RefreshTokenResponseDTO>.Fail("User not found.");

            var accessExpireAt = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
            var refreshExpireAt = DateTime.UtcNow.AddDays(_sessionSettings.RefreshTokenDays);

            var rawRefreshToken = GenerateSecureToken();

            var session = new UserSession
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                RefreshTokenHash = _dataHasher.HashData(rawRefreshToken),
                RefreshTokenExpiresAt = refreshExpireAt,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true,
                IpAddress = ipAddress,
                DeviceName = deviceName
            };

            _db.UserSessions.Add(session);

            user.Login = true;
            user.LastLogin = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var accessToken = GenerateAccessToken(user, session.SessionId, accessExpireAt);

            return ResponceApi<RefreshTokenResponseDTO>.Ok(new RefreshTokenResponseDTO
            {
                AccessToken = accessToken,
                AccessTokenExpiresAt = accessExpireAt,
                RefreshToken = rawRefreshToken,
                RefreshTokenExpiresAt = refreshExpireAt,
                SessionId = session.SessionId
            }, "Login successful.");
        }

        public async Task<ResponceApi<RefreshTokenResponseDTO>> RefreshSessionAsync(RefreshTokenRequestDTO dto)
        {
            var session = await _db.UserSessions.Include(s => s.User).FirstOrDefaultAsync(s => s.SessionId == dto.SessionId);

            if (session == null)
                return ResponceApi<RefreshTokenResponseDTO>.Fail("Session not found.");

            if (!session.IsActive)
                return ResponceApi<RefreshTokenResponseDTO>.Fail("Session is no longer active.");

            if (session.RefreshTokenExpiresAt <= DateTime.UtcNow)
            {
                session.IsActive = false;
                await _db.SaveChangesAsync();
                return ResponceApi<RefreshTokenResponseDTO>.Fail("Refresh token expired.");
            }

            if (session.LastActivityAt.AddMinutes(_sessionSettings.IdleTimeoutMinutes) <= DateTime.UtcNow)
            {
                session.IsActive = false;
                await _db.SaveChangesAsync();
                return ResponceApi<RefreshTokenResponseDTO>.Fail("Session expired due to inactivity.");
            }

            if (session.User.Blocked)
                return ResponceApi<RefreshTokenResponseDTO>.Fail("Account is blocked.");

            if (!session.User.Login)
                return ResponceApi<RefreshTokenResponseDTO>.Fail("User is logged out.");

            var tokenValid = _dataHasher.VerifyHashed(dto.RefreshToken, session.RefreshTokenHash);

            if (!tokenValid)
                return ResponceApi<RefreshTokenResponseDTO>.Fail("Invalid refresh token.");

            var newRawRefreshToken = GenerateSecureToken();
            var newRefreshExpireAt = DateTime.UtcNow.AddDays(_sessionSettings.RefreshTokenDays);
            var newAccessExpireAt = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);

            session.RefreshTokenHash = _dataHasher.HashData(newRawRefreshToken);
            session.RefreshTokenExpiresAt = newRefreshExpireAt;
            session.LastActivityAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            var newAccessToken = GenerateAccessToken(session.User, session.SessionId, newAccessExpireAt);

            return ResponceApi<RefreshTokenResponseDTO>.Ok(new RefreshTokenResponseDTO
            {
                AccessToken = newAccessToken,
                AccessTokenExpiresAt = newAccessExpireAt,
                RefreshToken = newRawRefreshToken,
                RefreshTokenExpiresAt = newRefreshExpireAt,
                SessionId = session.SessionId
            }, "Token refreshed successfully.");
        }

        public async Task<ResponceApi<bool>> RevokeSessionAsync(Guid sessionId)
        {
            var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId);
            if (session == null)
                return ResponceApi<bool>.Fail("Session not found.");

            session.IsActive = false;
            await _db.SaveChangesAsync();

            return ResponceApi<bool>.Ok(true, "Session revoked.");
        }

        public async Task<ResponceApi<bool>> RevokeAllUserSessionsAsync(Guid userId)
        {
            var sessions = await _db.UserSessions.Where(s => s.UserId == userId && s.IsActive).ToListAsync();

            foreach (var session in sessions)
                session.IsActive = false;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user != null)
                user.Login = false;

            await _db.SaveChangesAsync();

            return ResponceApi<bool>.Ok(true, "All sessions revoked.");
        }

        public async Task TouchSessionActivityAsync(Guid sessionId)
        {
            var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);

            if (session == null)
                return;

            if (session.LastActivityAt.AddMinutes(_sessionSettings.IdleTimeoutMinutes) <= DateTime.UtcNow)
            {
                session.IsActive = false;
                await _db.SaveChangesAsync();
                return;
            }

            session.LastActivityAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        private string GenerateAccessToken(User user, Guid sessionId, DateTime expireAt)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, _dataCiphers.Decrypt(user.TypeChipher) ?? "Viewer"),
                new Claim("session_id", sessionId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(issuer: _jwtSettings.Issuer, audience: _jwtSettings.Audience, claims: claims, expires: expireAt, signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateSecureToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}