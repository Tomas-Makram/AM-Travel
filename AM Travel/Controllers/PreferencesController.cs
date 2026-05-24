using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace AM_Travel.Controllers
{
    [AllowAnonymous]
    public class PreferencesController : Controller
    {
        [HttpPost]
        public IActionResult SetTheme(string theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
            {
                theme = "light";
            }

            Response.Cookies.Append(
                "theme",
                theme,
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = false,
                    IsEssential = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpGet]
        public IActionResult GetCurrentTheme()
        {
            var theme = Request.Cookies["theme"] ?? "light";

            return Json(new
            {
                theme
            });
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture)
        {
            var supportedCultures = new[] { "en", "ar", "fr" };

            if (!supportedCultures.Contains(culture))
            {
                culture = "en";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpGet]
        public IActionResult GetCurrentLanguage()
        {
            var culture = Request.HttpContext.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture.Name ?? "en";

            return Json(new
            {
                culture
            });
        }
    }
}