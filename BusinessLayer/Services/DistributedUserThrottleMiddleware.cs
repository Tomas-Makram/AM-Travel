using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Services
{
    public class UserThrottleOptions
    {
        public bool Enabled { get; set; } = true;
        public int PermitLimit { get; set; } = 100;
        public int WindowMinutes { get; set; } = 1;

        public string[] ProtectedPaths { get; set; } = Array.Empty<string>();

        public string[] ExcludedPaths { get; set; } = new[]
        {
            "/swagger",
            "/health"
        };
    }

    public class DistributedUserThrottleMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISecurityCounterStore _counterStore;
        private readonly UserThrottleOptions _options;
        private readonly ILogger<DistributedUserThrottleMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public DistributedUserThrottleMiddleware(RequestDelegate next, ISecurityCounterStore counterStore, IOptions<UserThrottleOptions> options, ILogger<DistributedUserThrottleMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _counterStore = counterStore;
            _options = options.Value;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!_options.Enabled)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            if (ShouldSkip(path) || !ShouldProtect(path))
            {
                await _next(context);
                return;
            }

            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var clientIp = GetClientIp(context);

            var identityKey = !string.IsNullOrWhiteSpace(userId) ? $"user:{userId}" : $"ip:{clientIp}";

            var window = TimeSpan.FromMinutes(_options.WindowMinutes);

            var bucket = DateTime.UtcNow.ToString("yyyyMMddHHmm");
            var normalizedPath = NormalizePath(path);

            var key = $"throttle:{identityKey}:path:{normalizedPath}:{bucket}";
            var count = await _counterStore.IncrementAsync(key, window, context.RequestAborted);

            if (_environment.IsDevelopment())
            {
                _logger.LogDebug(
                    "Throttle check. Path={Path}, IdentityType={IdentityType}, Count={Count}",
                    path,
                    string.IsNullOrWhiteSpace(userId) ? "ip" : "user",
                    count);
            }

            if (count > _options.PermitLimit)
            {
                var retryAfterSeconds = Math.Max(1, (int)window.TotalSeconds);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.ContentType = "application/json";
                context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

                _logger.LogWarning(
                    "Rate limit exceeded. Path={Path}, IdentityType={IdentityType}, Count={Count}, RetryAfter={RetryAfterSeconds}s",
                    path,
                    string.IsNullOrWhiteSpace(userId) ? "ip" : "user",
                    count,
                    retryAfterSeconds);

                var body = JsonSerializer.Serialize(new
                {
                    message = "Too many requests. Please try again later."
                });

                await context.Response.WriteAsync(body, context.RequestAborted);
                return;
            }

            await _next(context);
        }

        private bool ShouldSkip(string path)
        {
            if (_options.ExcludedPaths is null || _options.ExcludedPaths.Length == 0)
                return false;

            return _options.ExcludedPaths.Any(excluded =>
                !string.IsNullOrWhiteSpace(excluded) &&
                path.StartsWith(excluded.Trim().ToLowerInvariant(), StringComparison.Ordinal));
        }

        private bool ShouldProtect(string path)
        {
            if (_options.ProtectedPaths is null || _options.ProtectedPaths.Length == 0)
                return true;

            return _options.ProtectedPaths.Any(protectedPath =>
                !string.IsNullOrWhiteSpace(protectedPath) &&
                path.StartsWith(protectedPath.Trim().ToLowerInvariant(), StringComparison.Ordinal));
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "unknown-path";

            return path.Replace("/", ":");
        }

        private static string GetClientIp(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
                return forwardedFor.Split(',')[0].Trim();

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
                return realIp.Trim();

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        }
    }
}