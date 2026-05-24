using Microsoft.AspNetCore.Mvc;

namespace AM_Travel.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult StatusCodeHandler(int statusCode)
        {
            return statusCode switch
            {
                404 => View("NotFound"),
                403 => View("AccessDenied"),
                500 => View("ServerError"),
                _ => View("ServerError")
            };
        }

        [Route("Error/NotFound")]
        public IActionResult NotFoundPage() => View("NotFound");

        [Route("Error/AccessDenied")]
        public IActionResult AccessDenied() => View();

        [Route("Error/ServerError")]
        public IActionResult ServerError() => View();

        [Route("Error/TooManyRequests")]
        public IActionResult TooManyRequests(string? message, int retryAfter = 60, string? returnUrl = null)
        {
            Response.StatusCode = StatusCodes.Status429TooManyRequests;

            ViewBag.Message = string.IsNullOrWhiteSpace(message)
                ? $"Too many requests. Please wait {retryAfter} seconds before trying again."
                : message;

            ViewBag.RetryAfter = retryAfter;
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }
    }
}
