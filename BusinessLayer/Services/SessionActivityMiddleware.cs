using System.Security.Claims;
using BusinessLayer.Services;

namespace BusinessLayer.Services
{

    public class SessionActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITokenSessionService tokenSessionService)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var sessionIdClaim = context.User.FindFirst("session_id")?.Value;

                if (Guid.TryParse(sessionIdClaim, out var sessionId))
                {
                    await tokenSessionService.TouchSessionActivityAsync(sessionId);
                }
            }

            await _next(context);
        }
    }
}