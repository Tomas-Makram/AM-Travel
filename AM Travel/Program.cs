using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Asp.Versioning;
using BusinessLayer.Filters;
using BusinessLayer.Functions;
using BusinessLayer.Services;
using DataLayer.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using StackExchange.Redis;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;

namespace AM_Travel
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddOpenApi();

            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
            builder.Services.Configure<SessionSettings>(builder.Configuration.GetSection("SessionSettings"));
            builder.Services.Configure<PasswordHashSettings>(builder.Configuration.GetSection("PasswordHashSettings"));
            builder.Services.Configure<DataProtectionSettings>(builder.Configuration.GetSection("DataProtectionSettings"));
            builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("CacheSettings"));
            builder.Services.Configure<UserThrottleOptions>(builder.Configuration.GetSection("UserThrottle"));
            builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimitSettings"));
            builder.Services.Configure<AppInfoSettings>(builder.Configuration.GetSection("AppInfo"));
            builder.Services.Configure<TelegramSettings>(builder.Configuration.GetSection("TelegramSettings"));

            var appInfo = builder.Configuration.GetSection("AppInfo").Get<AppInfoSettings>() ?? new AppInfoSettings
            {
                Name = "AM Travel",
                Version = "1"
            };

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc($"v{appInfo.Version}", new OpenApiInfo
                {
                    Title = appInfo.Name,
                    Version = appInfo.Version
                });
            });

            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            builder.Services.AddDbContext<DBContext>(options =>
            {
                options.UseSqlServer(
                    builder.Configuration.GetConnectionString("Connection"),
                    sql => sql.MigrationsAssembly("AM Travel"));
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "AMTravel.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Error/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
                    options.SlidingExpiration = true;
                });
            builder.Services.AddLocalization(options => options.ResourcesPath = "");
            builder.Services.AddControllersWithViews().AddViewLocalization().AddDataAnnotationsLocalization();
            builder.Services.AddAuthorization();
            builder.Services.AddScoped<RequireActiveLoginFilter>();
            builder.Services.AddScoped<AdminSeeder>();
            builder.Services.AddScoped<IAuthenticateManager, AuthenticateManager>();
            builder.Services.AddScoped<IAdminManager, AdminManager>();
            builder.Services.AddHttpClient<ITelegramService, TelegramService>();
            builder.Services.AddScoped<IBookingManager, BookingManager>();
            builder.Services.AddScoped<ITransportationManager, TransportationManager>();
            builder.Services.AddScoped<ICompanySeatBookingManager, CompanySeatBookingManager>();
            builder.Services.AddScoped<IWorksManager, WorksManager>();
            builder.Services.AddScoped<CairoTimeService>();
            builder.Services.AddScoped<ITokenSessionService, TokenSessionService>();
            var keysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
            Directory.CreateDirectory(keysPath);

            builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath)).SetApplicationName("AMTravel");
            builder.Services.AddSingleton<IDataCiphers, DataCiphers>();
            builder.Services.AddSingleton<IDataHasher, DataHasher>();
            builder.Services.AddAppRateLimiting(builder.Configuration);
            builder.Services.AddMemoryCache();

            var cacheSettings = builder.Configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? new CacheSettings();
            if (cacheSettings.UseRedis)
            {
                var redisOptions = ConfigurationOptions.Parse(cacheSettings.RedisConnection);
                redisOptions.AbortOnConnectFail = false;
                redisOptions.ConnectRetry = 3;
                redisOptions.ConnectTimeout = 5000;
                redisOptions.SyncTimeout = 5000;
                var redis = ConnectionMultiplexer.Connect(redisOptions);
                builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
                builder.Services.AddSingleton<ISecurityCounterStore, RedisSecurityCounterStore>();
            }
            else
            {
                builder.Services.AddSingleton<ISecurityCounterStore, MemorySecurityCounterStore>();
            }

            builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

            var app = builder.Build();
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders =
                 ForwardedHeaders.XForwardedFor |
                 ForwardedHeaders.XForwardedProto
            };

            forwardedHeadersOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedHeadersOptions);

            var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ar"), new CultureInfo("fr") };
            app.UseRequestLocalization(new RequestLocalizationOptions { DefaultRequestCulture = new RequestCulture("en"), SupportedCultures = supportedCultures, SupportedUICultures = supportedCultures });


            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var adminSeeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
                    await adminSeeder.SeedAdminAsync();
                }
                catch { }
            }

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(u =>
                {
                    u.SwaggerEndpoint($"/swagger/v{appInfo.Version}/swagger.json", $"{appInfo.Name} v{appInfo.Version}");
                });
            }
            else
            {
                app.UseExceptionHandler("/Error/ServerError");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Error/{0}");
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseRateLimiter();
            app.UseAuthentication();
            app.UseMiddleware<SessionActivityMiddleware>();
            app.UseMiddleware<DistributedUserThrottleMiddleware>();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
