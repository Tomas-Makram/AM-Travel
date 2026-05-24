using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace BusinessLayer.Services
{
    public static class RateLimitSetup
    {
        public const string ApiDefaultPolicy = "api-default-policy";
        public const string LoginPolicy = "login-policy";
        public const string RegisterPolicy = "register-policy";
        public const string RefreshPolicy = "refresh-policy";
        public const string LogoutPolicy = "logout-policy";
        public const string HeavyOpsPolicy = "heavy-ops-policy";

        public const string AuthenticatedUserPolicy = "authenticated-user-policy";
        public const string PerUserHeavyOpsPolicy = "per-user-heavy-ops-policy";
        public const string BurstPerPathPolicy = "burst-per-path-policy";

        private const string LoginUsernameItemKey = "LoginRateLimitUsername";

        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var settings = configuration.GetSection("RateLimitSettings").Get<RateLimitSettings>() ?? new RateLimitSettings();

            services.AddRateLimiter((options) =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.OnRejected = async (context, token) =>
                {
                    var http = context.HttpContext;

                    var fallbackSeconds = GetFallbackRetryAfterSeconds(http, settings);
                    var retryAfterSeconds = fallbackSeconds;

                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
                    }

                    http.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

                    var message = $"{settings.RejectionMessage} Please wait {FormatWaitTime(retryAfterSeconds)} before trying again.";
                    var encodedMessage = Uri.EscapeDataString(message);
                    var returnUrl = Uri.EscapeDataString(http.Request.Path + http.Request.QueryString);

                    http.Response.Redirect($"/Error/TooManyRequests?message={encodedMessage}&retryAfter={retryAfterSeconds}&returnUrl={returnUrl}");

                    await Task.CompletedTask;
                };

                //options.OnRejected = async (context, token) =>
                //{
                //    var http = context.HttpContext;
                //    http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                //    http.Response.ContentType = "application/json";

                //    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                //    {
                //        http.Response.Headers.RetryAfter =
                //            Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                //    }

                //    await http.Response.WriteAsync(
                //        $"{{\"message\":\"{EscapeJson(settings.RejectionMessage)}\"}}",
                //        token);
                //};

                // Global limiter by IP
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var ip = GetClientIp(context);

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"global:{ip}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = settings.GlobalLimiter.PermitLimit,
                            Window = TimeSpan.FromMinutes(settings.GlobalLimiter.WindowMinutes),
                            SegmentsPerWindow = settings.GlobalLimiter.SegmentsPerWindow,
                            QueueLimit = settings.GlobalLimiter.QueueLimit,
                            AutoReplenishment = true
                        });
                });

                // API default by IP + path
                options.AddPolicy(ApiDefaultPolicy, context =>
                {
                    var ip = GetClientIp(context);
                    var path = NormalizePath(context.Request.Path.Value);

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"api:{ip}:{path}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = settings.ApiDefaultPolicy.PermitLimit,
                            Window = TimeSpan.FromMinutes(settings.ApiDefaultPolicy.WindowMinutes),
                            SegmentsPerWindow = settings.ApiDefaultPolicy.SegmentsPerWindow,
                            QueueLimit = settings.ApiDefaultPolicy.QueueLimit,
                            AutoReplenishment = true
                        });
                });

                // Authenticated users
                options.AddPolicy(AuthenticatedUserPolicy, context =>
                {
                    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var ip = GetClientIp(context);

                    var key = !string.IsNullOrWhiteSpace(userId) ? $"auth-user:{userId}" : $"auth-fallback-ip:{ip}";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: key,
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = settings.AuthenticatedUserPolicy.PermitLimit,
                            Window = TimeSpan.FromMinutes(settings.AuthenticatedUserPolicy.WindowMinutes),
                            SegmentsPerWindow = settings.AuthenticatedUserPolicy.SegmentsPerWindow,
                            QueueLimit = settings.AuthenticatedUserPolicy.QueueLimit,
                            AutoReplenishment = true
                        });
                });

                // Heavy operations per authenticated user
                options.AddPolicy(PerUserHeavyOpsPolicy, context =>
                {
                    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var ip = GetClientIp(context);

                    var key = !string.IsNullOrWhiteSpace(userId) ? $"heavy-user:{userId}" : $"heavy-ip:{ip}";

                    return RateLimitPartition.GetConcurrencyLimiter(
                        partitionKey: key,
                        factory: _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = settings.PerUserHeavyOpsPolicy.PermitLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = settings.PerUserHeavyOpsPolicy.QueueLimit
                        });
                });

                // Login policy
                options.AddPolicy(LoginPolicy, context =>
                {
                    var ip = GetClientIp(context);
                    var username = Normalize(
                        context.Items[LoginUsernameItemKey]?.ToString(),
                        string.Empty);

                    var key = !string.IsNullOrWhiteSpace(username) ? $"login:user:{username}" : $"login:ip:{ip}";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: key,
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = settings.LoginPolicy.PermitLimit,
                            Window = TimeSpan.FromMinutes(settings.LoginPolicy.WindowMinutes),
                            SegmentsPerWindow = settings.LoginPolicy.SegmentsPerWindow,
                            QueueLimit = settings.LoginPolicy.QueueLimit,
                            AutoReplenishment = true
                        });
                });

                // Register policy
                options.AddPolicy(RegisterPolicy, context =>
                {
                    var ip = GetClientIp(context);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"register:{ip}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.RegisterPolicy.PermitLimit,
                            Window = TimeSpan.FromMinutes(settings.RegisterPolicy.WindowMinutes),
                            QueueLimit = settings.RegisterPolicy.QueueLimit,
                            AutoReplenishment = true
                        });
                });

                // Refresh policy
                options.AddPolicy(RefreshPolicy, context =>
                {
                    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var ip = GetClientIp(context);

                    var key = !string.IsNullOrWhiteSpace(userId)
                        ? $"refresh:user:{userId}"
                        : $"refresh:ip:{ip}";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: key,
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = settings.RefreshPolicy.PermitLimit,
                            Window = TimeSpan.FromMinutes(settings.RefreshPolicy.WindowMinutes),
                            SegmentsPerWindow = settings.RefreshPolicy.SegmentsPerWindow,
                            QueueLimit = settings.RefreshPolicy.QueueLimit,
                            AutoReplenishment = true
                        });
                });

                // Logout policy
                options.AddPolicy(LogoutPolicy, context =>
                {
                    var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var ip = GetClientIp(context);

                    var key = !string.IsNullOrWhiteSpace(userId)
                        ? $"logout:user:{userId}"
                        : $"logout:ip:{ip}";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: key,
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = settings.LogoutPolicy.PermitLimit,
                            Window = TimeSpan.FromMinutes(settings.LogoutPolicy.WindowMinutes),
                            SegmentsPerWindow = settings.LogoutPolicy.SegmentsPerWindow,
                            QueueLimit = settings.LogoutPolicy.QueueLimit,
                            AutoReplenishment = true
                        });
                });

                // Global heavy ops
                options.AddPolicy(HeavyOpsPolicy, _ =>
                {
                    return RateLimitPartition.GetConcurrencyLimiter(
                        partitionKey: "heavy-ops",
                        factory: _ => new ConcurrencyLimiterOptions
                        {
                            PermitLimit = settings.HeavyOpsPolicy.PermitLimit,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = settings.HeavyOpsPolicy.QueueLimit
                        });
                });

                // Burst per path
                options.AddPolicy(BurstPerPathPolicy, context =>
                {
                    var ip = GetClientIp(context);
                    var path = NormalizePath(context.Request.Path.Value);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: $"burst:{ip}:{path}",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.BurstPerPathPolicy.PermitLimit,
                            Window = TimeSpan.FromMinutes(settings.BurstPerPathPolicy.WindowMinutes),
                            QueueLimit = settings.BurstPerPathPolicy.QueueLimit,
                            AutoReplenishment = true
                        });
                });
            });

            return services;
        }

        private static string GetClientIp(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                var firstIp = forwardedFor.Split(',')[0].Trim();
                return Normalize(firstIp, "unknown-ip");
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
            {
                return Normalize(realIp, "unknown-ip");
            }

            return Normalize(context.Connection.RemoteIpAddress?.ToString(), "unknown-ip");
        }

        private static string Normalize(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
        }

        private static string NormalizePath(string? path)
        {
            return string.IsNullOrWhiteSpace(path) ? "unknown-path" : path.Trim().ToLowerInvariant();
        }

        private static string EscapeJson(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static int GetFallbackRetryAfterSeconds(HttpContext context, RateLimitSettings settings)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            if (path.Contains("/auth/login"))
                return settings.LoginPolicy.WindowMinutes * 60;

            if (path.Contains("/auth/logout"))
                return settings.LogoutPolicy.WindowMinutes * 60;

            if (path.Contains("/auth/refresh"))
                return settings.RefreshPolicy.WindowMinutes * 60;

            if (path.Contains("/auth/register") || path.Contains("/createnewaccount"))
                return settings.RegisterPolicy.WindowMinutes * 60;

            if (path.Contains("/api/"))
                return settings.ApiDefaultPolicy.WindowMinutes * 60;

            return settings.GlobalLimiter.WindowMinutes * 60;
        }

        private static string FormatWaitTime(int seconds)
        {
            if (seconds < 60)
                return $"{seconds} second(s)";

            var minutes = (int)Math.Ceiling(seconds / 60.0);
            return $"{minutes} minute(s)";
        }
    }
}