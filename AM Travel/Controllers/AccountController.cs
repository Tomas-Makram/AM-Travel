using BusinessLayer.DTOs.Account;
using BusinessLayer.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AM_Travel.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IAuthenticateManager _authenticate;

        public AccountController(IAuthenticateManager authenticate)
        {
            _authenticate = authenticate;
        }

        private Guid GetCurrentUserId()
        {
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            return userId;
        }

        private string GetCurrentFullName()
        {
            return User.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {

            var result = await _authenticate.GetMyAccount(GetCurrentUserId());
            if (!result.Success || result.Data == null)
                return RedirectToAction("Login", "Auth");

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var result = await _authenticate.GetMyAccount(GetCurrentUserId());
            if (!result.Success || result.Data == null)
                return RedirectToAction("Login", "Auth");

            var model = new ChangeFieldsDTO
            {
                UserId = result.Data.UserId,
                FullName = result.Data.FullName,
                PhoneNumber = result.Data.PhoneNumber,
                NationalId = result.Data.NationalId
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ChangeFieldsDTO model)
        {
            model.UserId = GetCurrentUserId();

            if (!ModelState.IsValid)
                return View(model);

            var result = await _authenticate.ChangeFields(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Unable to update account.");
                return View(model);
            }

            TempData["Success"] = "Account updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewBag.FullName = GetCurrentFullName();
            return View(new ChangePasswordDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO model)
        {
            model.UserId = GetCurrentUserId();

            if (!ModelState.IsValid)
                return View(model);

            var result = await _authenticate.ChangePassword(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message ?? "Unable to change password.");
                return View(model);
            }

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
