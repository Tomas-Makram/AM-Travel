using BusinessLayer.DTOs.Account;
using BusinessLayer.Functions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AM_Travel.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminManager _adminManager;

        public AdminController(IAdminManager adminManager)
        {
            _adminManager = adminManager;
        }

        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized();

            var currentUserId = Guid.Parse(userIdClaim);
            var result = await _adminManager.GetAllUsersAsync(currentUserId);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(new List<AccountsDTO>());
            }

            return View(result.Data);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            return View(new CreateNewAccountDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateNewAccountDTO model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _adminManager.CreateUserAsync(model);

            if (!result.Success)
            {
                if (result.Errors != null)
                {
                    foreach (var error in result.Errors)
                        ModelState.AddModelError(string.Empty, error);
                }

                TempData["Error"] = result.Message;
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeBlockStatus(Guid userId)
        {

            var result = await _adminManager.ChangeBlockStatusAsync(userId);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeActivateStatus(Guid userId)
        {
            var result = await _adminManager.ChangeActivateStatusAsync(userId);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(Guid userId, string newPassword)
        {
            var result = await _adminManager.ChangePasswordAsync(userId, newPassword);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(Users));
        }
    }
}