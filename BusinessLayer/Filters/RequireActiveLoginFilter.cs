using BusinessLayer.Models;
using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BusinessLayer.Filters
{
    public class RequireActiveLoginFilter : IAsyncAuthorizationFilter
    {
        private readonly DBContext _db;
        private readonly SessionSettings _sessionSettings;

        public RequireActiveLoginFilter(DBContext db, IOptions<SessionSettings> sessionOptions)
        {
            _db = db;
            _sessionSettings = sessionOptions.Value;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult(ResponceApi<string>.Fail("You are not logged in", "Unauthorized"));
                return;
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var sessionIdClaim = user.FindFirst("session_id")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(ResponceApi<string>.Fail("The login code is invalid", "Invalid user id claim"));
                return;
            }

            if (!Guid.TryParse(sessionIdClaim, out var sessionId))
            {
                context.Result = new UnauthorizedObjectResult(ResponceApi<string>.Fail("The session code is invalid", "Invalid session id claim"));
                return;
            }

            var dbUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);

            if (dbUser == null)
            {
                context.Result = new UnauthorizedObjectResult(ResponceApi<string>.Fail("Account not found", "User not found"));
                return;
            }

            if (dbUser.Blocked)
            {
                context.Result = new ObjectResult(ResponceApi<string>.Fail("This account is blocked", "Blocked account"))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            var session = await _db.UserSessions.AsNoTracking().FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

            if (session == null || !session.IsActive)
            {
                context.Result = new UnauthorizedObjectResult(ResponceApi<string>.Fail("The session is inactive or you have logged out", "Inactive session"));
                return;
            }

            if (session.LastActivityAt.AddMinutes(_sessionSettings.IdleTimeoutMinutes) <= DateTime.UtcNow)
            {
                context.Result = new UnauthorizedObjectResult(ResponceApi<string>.Fail("The session ended due to inactivity", "Session expired due to inactivity"));
                return;
            }

            if (!dbUser.Login)
            {
                context.Result = new UnauthorizedObjectResult(ResponceApi<string>.Fail("You have been logged out. Please log in again", "Inactive login"));
                return;
            }
        }
    }
}