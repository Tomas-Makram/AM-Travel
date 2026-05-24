namespace BusinessLayer.Services
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _environment;

        public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
        {
            _next = next;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["X-Permitted-Cross-Domain-Policies"] = "none";

                headers["Permissions-Policy"] =
                    "accelerometer=(), camera=(), geolocation=(), gyroscope=(), " +
                    "magnetometer=(), microphone=(), payment=(), usb=()";

                headers["Cross-Origin-Opener-Policy"] = "same-origin";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";
                headers["Cross-Origin-Embedder-Policy"] = "require-corp";

                if (_environment.IsDevelopment())
                {
                    headers["Content-Security-Policy"] =
                        "default-src 'self'; " +
                        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data: https:; " +
                        "font-src 'self' data: https:; " +
                        "connect-src 'self' https:; " +
                        "frame-ancestors 'none'; " +
                        "base-uri 'self'; " +
                        "form-action 'self';";
                }
                else
                {
                    headers["Content-Security-Policy"] =
                        "default-src 'self'; " +
                        "script-src 'self'; " +
                        "style-src 'self'; " +
                        "img-src 'self' data:; " +
                        "font-src 'self'; " +
                        "connect-src 'self'; " + // "connect-src 'self' https://your-frontend.com; " +
                        "frame-ancestors 'none'; " +
                        "base-uri 'self'; " +
                        "form-action 'self'; " +
                        "object-src 'none';";

                    headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
                }

                headers.Remove("Server");

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }
}