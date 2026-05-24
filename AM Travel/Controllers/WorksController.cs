using BusinessLayer.DTOs.Work;
using BusinessLayer.Functions;
using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AM_Travel.Controllers
{
    [Authorize]
    public class WorksController : Controller
    {
        private readonly IWorksManager _worksManager;

        public WorksController(IWorksManager worksManager)
        {
            _worksManager = worksManager;
        }

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }

        private string GetCurrentRole()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? "Viewer";
        }

        private string GetCurrentFullName()
        {
            return User.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, DateTime? date, ClientType? clientType)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var result = await _worksManager.GetAllWorks(userId, role, search, date, clientType);

            ViewBag.Search = search;
            ViewBag.Date = date != null ? date?.ToString("yyyy-MM-dd") : DateTime.UtcNow.ToString("yyyy-MM-dd");
            ViewBag.ClientType = clientType;
            ViewBag.CurrentUserId = userId;
            ViewBag.Name = GetCurrentFullName();

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to load works.";
                return View(new List<WorkListItemDTO>());
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Today(string? search, ClientType? clientType)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var result = await _worksManager.GetWorksToday(userId, role, search, clientType);

            ViewBag.Search = search;
            ViewBag.ClientType = clientType;
            ViewBag.CurrentUserId = userId;
            ViewBag.Name = GetCurrentFullName();

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to load today works.";
                return View(new List<WorkListItemDTO>());
            }

            return View(result.Data);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Report(Guid? userId, string? search, DateTime? date, ClientType? clientType)
        {
            var usersResult = await _worksManager.GetReportUsers();
            var worksResult = await _worksManager.GetReportWorks(userId, search, date != null ? date : DateTime.UtcNow, clientType);

            ViewBag.Users = usersResult.Success && usersResult.Data != null ? usersResult.Data : new List<WorkReportUserDTO>();

            ViewBag.SelectedUserId = userId;
            ViewBag.Search = search;
            ViewBag.Date = date != null ? date?.ToString("yyyy-MM-dd") : DateTime.UtcNow.ToString("yyyy-MM-dd");
            ViewBag.ClientType = clientType;
            ViewBag.CurrentUserId = GetCurrentUserId();
            ViewBag.Name = GetCurrentFullName();

            if (!usersResult.Success)
                TempData["Error"] = usersResult.Message ?? "Failed to load users.";

            if (!worksResult.Success || worksResult.Data == null)
            {
                TempData["Error"] = worksResult.Message ?? "Failed to load report works.";
                return View(new List<WorkListItemDTO>());
            }

            return View(worksResult.Data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Name = GetCurrentFullName();

            return View(new CreateWorkDTO
            {
                DayUpdated = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateWorkDTO model)
        {
            ViewBag.Name = GetCurrentFullName();

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            var result = await _worksManager.CreateNewWorks(model, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.Message ?? "Failed to create work.";
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            ViewBag.Name = GetCurrentFullName();

            var userId = GetCurrentUserId();

            var result = await _worksManager.GetUpdateWorks(id, userId);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "You can edit only your own works.";
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateWorkDTO model)
        {
            ViewBag.Name = GetCurrentFullName();

            if (!ModelState.IsValid)
                return View(model);

            var userId = GetCurrentUserId();

            var result = await _worksManager.UpdateWorks(model, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.Message ?? "You can edit only your own works.";
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();

            var result = await _worksManager.DeleteWorks(id, userId);

            TempData[result.Success ? "Success" : "Error"] =
                result.Message ?? (result.Success ? "Work deleted successfully." : "Failed to delete work.");

            return RedirectToAction(nameof(Index));
        }
    }
}