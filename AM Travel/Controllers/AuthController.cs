using BusinessLayer.DTOs.Account;
using BusinessLayer.DTOs.Auth;
using BusinessLayer.Functions;
using BusinessLayer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AM_Travel.Controllers
{
//    [Authorize(Roles ="Tomas")]
    public class AuthController : Controller
    {
        private readonly IAuthenticateManager _authenticate;

        public AuthController(IAuthenticateManager authenticate)
        {
            _authenticate = authenticate;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginDTO());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitSetup.LoginPolicy)]
        public async Task<IActionResult> Login(LoginDTO model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var result = await _authenticate.Login(model);
            if (!result.Success || result.Data == null)
            {
                var error = result.Message ?? "Login failed.";

                //if (result.Errors != null && result.Errors.Any())
                //    error += " - " + string.Join(" | ", result.Errors);

                ModelState.AddModelError(string.Empty, error);
                return View(model);
            }

            var claims = BuildCookieClaims(result.Data);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = result.Data.ExpireAt.ToUniversalTime()
                });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(RateLimitSetup.LogoutPolicy)]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionIdClaim = User.FindFirstValue("session_id");

            if (Guid.TryParse(userIdClaim, out var userId) && Guid.TryParse(sessionIdClaim, out var sessionId))
                await _authenticate.Logout(userId, sessionId);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private static List<Claim> BuildCookieClaims(LoginResponceDTO data)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(data.Token);

            var claims = new List<Claim>();

            foreach (var claim in jwt.Claims)
            {
                var type = claim.Type switch
                {
                    "nameid" => ClaimTypes.NameIdentifier,
                    "unique_name" => ClaimTypes.Name,
                    "name" => ClaimTypes.Name,
                    "role" => ClaimTypes.Role,
                    "given_name" => ClaimTypes.GivenName,
                    _ => claim.Type
                };

                if (!claims.Any(c => c.Type == type && c.Value == claim.Value))
                    claims.Add(new Claim(type, claim.Value));
            }

            if (!claims.Any(c => c.Type == "access_token"))
                claims.Add(new Claim("access_token", data.Token));

            if (!claims.Any(c => c.Type == "refresh_token"))
                claims.Add(new Claim("refresh_token", data.RefreshToken ?? string.Empty));

            return claims;
        }

        // ---------------------------------- Devoloper Tools ----------------------------------//
        
        [HttpGet]
        [AllowAnonymous]
        public IActionResult DevPortal()
        {
            if (IsDevFullyVerified())
                return RedirectToAction(nameof(DevUsers));
            ViewBag.OtpExpireMinutes = _authenticate.GetOtpExpireMinutes();
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DevPortalSendOtp(string secretCode)
        {
            if (string.IsNullOrWhiteSpace(secretCode) || secretCode.Trim() != "Tom_866270")
            {
                TempData["DevError"] = "Wrong secret code.";
                return RedirectToAction(nameof(DevPortal));
            }

            var result = await _authenticate.SendTelegramOtp();

            if (!result.Success)
            {
                TempData["DevError"] = result.Message;
                return RedirectToAction(nameof(DevPortal));
            }

            Response.Cookies.Append("DEV_CODE_VERIFIED", "true", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(15),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

            Response.Cookies.Append("DEV_CODE_VERIFIED_AT", DateTime.UtcNow.ToString("O"), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(15),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

            TempData["DevSuccess"] = "OTP sent successfully on Telegram.";
            return RedirectToAction(nameof(DevPortal));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DevPortalVerifyOtp(string otpCode)
        {
            if (!IsDevCodeVerified())
            {
                TempData["DevError"] = "Secret code not verified.";
                return RedirectToAction(nameof(DevPortal));
            }

            if (string.IsNullOrWhiteSpace(otpCode))
            {
                TempData["DevError"] = "Enter OTP code.";
                return RedirectToAction(nameof(DevPortal));
            }

            var result = await _authenticate.VerifyTelegramOtp(otpCode.Trim());

            if (!result.Success)
            {
                TempData["DevError"] = result.Message;
                return RedirectToAction(nameof(DevPortal));
            }

            Response.Cookies.Append("DEV_OTP_VERIFIED", "true", new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

            Response.Cookies.Append("DEV_OTP_VERIFIED_AT", DateTime.UtcNow.ToString("O"), new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

            return RedirectToAction(nameof(DevUsers));
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> DevUsers()
        {
            if (!IsDevFullyVerified())
                return RedirectToAction(nameof(DevPortal));

            var result = await _authenticate.GetAllAccounts();

            if (!result.Success || result.Data == null)
            {
                TempData["DevError"] = "Failed to load users.";
                return RedirectToAction(nameof(DevPortal));
            }

            return View("DevPortalUsers", result.Data);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DevLoginAsUser(Guid userId)
        {
            if (!IsDevFullyVerified())
                return RedirectToAction(nameof(DevPortal));

            var result = await _authenticate.LoginAsUser(userId);

            if (!result.Success || result.Data == null)
            {
                TempData["DevError"] = result.Message ?? "Failed to login.";
                return RedirectToAction(nameof(DevUsers));
            }

            var claims = BuildCookieClaims(result.Data);
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = result.Data.ExpireAt.ToUniversalTime()
                });

            // Clear Dev session
            Response.Cookies.Delete("DEV_CODE_VERIFIED");
            Response.Cookies.Delete("DEV_CODE_VERIFIED_AT");
            Response.Cookies.Delete("DEV_OTP_VERIFIED");
            Response.Cookies.Delete("DEV_OTP_VERIFIED_AT");

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DevChangePassword(Guid userId, string newPassword)
        {
            if (!IsDevFullyVerified())
                return RedirectToAction(nameof(DevPortal));

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["DevError"] = "Password must be at least 6 characters.";
                return RedirectToAction(nameof(DevUsers));
            }

            var adminManager = HttpContext.RequestServices.GetRequiredService<IAdminManager>();
            var result = await adminManager.ChangePasswordAsync(userId, newPassword.Trim(), true);

            TempData[result.Success ? "DevSuccess" : "DevError"] = result.Message;
            return RedirectToAction(nameof(DevUsers));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeActivateStatus(Guid userId)
        {
            if (!IsDevFullyVerified())
                return RedirectToAction(nameof(DevPortal));

            var adminManager = HttpContext.RequestServices.GetRequiredService<IAdminManager>();
            var result = await adminManager.ChangeActivateStatusAsync(userId, true);

            TempData[result.Success ? "DevSuccess" : "DevError"] = result.Message;
            return RedirectToAction(nameof(DevUsers));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeBlockStatus(Guid userId)
        {
            if (!IsDevFullyVerified())
                return RedirectToAction(nameof(DevPortal));

            var adminManager = HttpContext.RequestServices.GetRequiredService<IAdminManager>();
            var result = await adminManager.ChangeBlockStatusAsync(userId, true);

            TempData[result.Success ? "DevSuccess" : "DevError"] = result.Message;
            return RedirectToAction(nameof(DevUsers));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult DevLogout()
        {
            HttpContext.Session.Remove("DEV_CODE_VERIFIED");
            HttpContext.Session.Remove("DEV_CODE_VERIFIED_AT");
            HttpContext.Session.Remove("DEV_OTP_VERIFIED");
            HttpContext.Session.Remove("DEV_OTP_VERIFIED_AT");
            return RedirectToAction(nameof(DevPortal));
        }

        private bool IsDevCodeVerified()
        {
            var v = Request.Cookies["DEV_CODE_VERIFIED"];
            if (v != "true") return false;

            var atText = Request.Cookies["DEV_CODE_VERIFIED_AT"];
            if (!DateTime.TryParse(atText, out var at)) return false;

            return DateTime.UtcNow - at <= TimeSpan.FromMinutes(15);
        }

        private bool IsDevFullyVerified()
        {
            // OTP must be verified
            var v = Request.Cookies["DEV_OTP_VERIFIED"];
            if (v != "true") return false;

            var atText = Request.Cookies["DEV_OTP_VERIFIED_AT"];
            if (!DateTime.TryParse(atText, out var at)) return false;

            return DateTime.UtcNow - at <= TimeSpan.FromMinutes(30);
        }
    }
}