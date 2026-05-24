using BusinessLayer.DTOs;
using BusinessLayer.DTOs.Book;
using BusinessLayer.Functions;
using DataLayer.Models;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AM_Travel.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly IBookingManager _bookingManager;

        public BookingsController(IBookingManager bookingManager, DBContext db)
        {
            _bookingManager = bookingManager;
        }

        private Guid GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }

        private string GetCurrentFullName() => User.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";

        private async Task FillLookups()
        {
            var lookups = await _bookingManager.GetFormLookupsAsync();

            ViewBag.RoomTypes = lookups.RoomTypes
                .Select(x => new SelectListItem { Value = x.Value, Text = x.Text })
                .ToList();

            ViewBag.PayTypes = lookups.PayTypes
                .Select(x => new SelectListItem { Value = x.Value, Text = x.Text })
                .ToList();

            ViewBag.BusRoutes = lookups.BusRoutes;

            ViewBag.Buses = lookups.BusRoutes
                .Select(x => new SelectListItem
                {
                    Value = x.BusId.ToString(),
                    Text = $"{x.BusName}{(string.IsNullOrWhiteSpace(x.PlateNumber) ? "" : $" ({x.PlateNumber})")} - {x.FromLocation} -> {x.ToLocation}"
                })
                .ToList();

            ViewBag.FromLocations = lookups.FromLocations;
        }

        private void AddErrorsToModelState(List<string>? errors, string? fallback)
        {
            if (errors == null || errors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, fallback ?? "Operation failed.");
                return;
            }

            foreach (var error in errors)
                ModelState.AddModelError(string.Empty, error);
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> DailyWork(DateTime? date, string workType = "transport", TripDirection? direction = null, string hotelDateType = "CheckIn")
        {
            var selectedDate = date?.Date ?? DateTime.Today;

            workType = string.IsNullOrWhiteSpace(workType)
                ? "transport"
                : workType.Trim().ToLower();

            if (workType != "hotel" && workType != "transport")
                workType = "transport";

            if (!hotelDateType.Equals("CheckOut", StringComparison.OrdinalIgnoreCase))
                hotelDateType = "CheckIn";

            var filter = new BookingDailyWorkFilterDTO
            {
                SelectedDate = selectedDate,
                WorkType = workType,
                Direction = direction ?? TripDirection.Departure,
                HotelDateType = hotelDateType
            };

            var result = await _bookingManager.GetDailyWorkAsync(filter);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to load daily work.";

                return View(new BookingDailyWorkPageDTO
                {
                    SelectedDate = selectedDate,
                    WorkType = workType,
                    Direction = filter.Direction,
                    HotelDateType = hotelDateType
                });
            }

            ViewBag.Name = GetCurrentFullName();
            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> Index(string? search, DateTime? checkInDate, DateTime? checkOutDate, string? bookingType)
        {
            ViewBag.Search = search;
            ViewBag.CheckInDate = checkInDate?.ToString("yyyy-MM-dd");
            ViewBag.CheckOutDate = checkOutDate?.ToString("yyyy-MM-dd");
            ViewBag.BookingType = bookingType;
            ViewBag.Name = GetCurrentFullName();

            var result = await _bookingManager.GetAllAsync(search, checkInDate, checkOutDate, bookingType);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(new List<BookingListItemDTO>());
            }

            return View(result.Data ?? new List<BookingListItemDTO>());
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> BookingUserInfo(Guid bookingId)
        {
            var result = await _bookingManager.GetBookingUserInfoAsync(bookingId);

            if (!result.Success || result.Data == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message ?? "Booking user not found."
                });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    fullName = result.Data.FullName,
                    userName = result.Data.UserName,
                    phoneNumber = result.Data.PhoneNumber
                }
            });
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _bookingManager.GetByIdAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> Report(Guid id)
        {
            var result = await _bookingManager.GetByIdAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Booking not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> Ticket(Guid id)
        {
            var result = await _bookingManager.GetByIdAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPayment(Guid id, AddBookingPaymentDTO dto)
        {
            if (id == Guid.Empty)
            {
                TempData["Error"] = "Booking id is required.";
                return RedirectToAction(nameof(Index));
            }

            dto.BookingId = id;

            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage));

                return RedirectToAction(nameof(Details), new { id });
            }

            var userId = GetCurrentUserId();

            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            var result = await _bookingManager.AddPaymentAsync(dto, userId);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> Audit(Guid id)
        {
            var result = await _bookingManager.GetByIdAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await FillLookups();
            ViewBag.Name = GetCurrentFullName();
            return View(_bookingManager.GetCreateDefaults());
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingDTO dto)
        {
            PrepareDtoForSubmit(dto);

            ModelState.Clear();

            if (!TryValidateModel(dto))
            {
                PrepareDtoForRedisplay(dto);
                await FillLookups();
                ViewBag.Name = GetCurrentFullName();
                return View(dto);
            }

            var userId = GetCurrentUserId();

            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            var result = await _bookingManager.CreateAsync(dto, userId);

            if (!result.Success)
            {
                AddErrorsToModelState(result.Errors, result.Message);

                PrepareDtoForRedisplay(dto);
                await FillLookups();
                ViewBag.Name = GetCurrentFullName();
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = result.Data });
        }

        private void PrepareDtoForSubmit(CreateBookingDTO dto)
        {
            dto.ClientName = dto.ClientName?.Trim() ?? string.Empty;
            dto.HotelName = dto.HotelName?.Trim() ?? string.Empty;
            dto.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

            dto.PhoneNumbers ??= new List<BookingPhoneDTO>();
            dto.Rooms ??= new List<BookingRoomDTO>();
            dto.Payments ??= new List<BookingPaymentDTO>();
            dto.TransportationSeats ??= new List<BookingSeatDTO>();
            dto.Transportation ??= new TransportationBookingDTO();

            dto.Transportation.DepartureSeats ??= new List<BookingSeatDTO>();
            dto.Transportation.ReturnSeats ??= new List<BookingSeatDTO>();

            if (!User.IsInRole("Admin"))
                dto.Discount = 0;

            dto.PhoneNumbers = dto.PhoneNumbers
                .Where(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .Select(x =>
                {
                    x.PhoneNumber = x.PhoneNumber.Trim();
                    return x;
                })
                .ToList();

            if (dto.PhoneNumbers.Any() && !dto.PhoneNumbers.Any(x => x.Prime))
                dto.PhoneNumbers[0].Prime = true;

            dto.Payments = dto.Payments
                .Where(x => x.Amount > 0 || !string.IsNullOrWhiteSpace(x.Notes))
                .Select(x =>
                {
                    x.Notes = string.IsNullOrWhiteSpace(x.Notes) ? null : x.Notes.Trim();
                    return x;
                })
                .ToList();

            dto.TransportationSeats.Clear();

            if (dto.HasTransportation)
            {
                if (dto.Transportation.TripType == TransportationTripType.Departure ||
                    dto.Transportation.TripType == TransportationTripType.RoundTrip)
                {
                    foreach (var seat in dto.Transportation.DepartureSeats.Where(x => x.SeatId != Guid.Empty))
                    {
                        seat.Direction = TripDirection.Departure;
                        dto.TransportationSeats.Add(seat);
                    }
                }

                if (dto.Transportation.TripType == TransportationTripType.Return ||
                    dto.Transportation.TripType == TransportationTripType.RoundTrip)
                {
                    foreach (var seat in dto.Transportation.ReturnSeats.Where(x => x.SeatId != Guid.Empty))
                    {
                        seat.Direction = TripDirection.Return;
                        dto.TransportationSeats.Add(seat);
                    }
                }
            }

            dto.NightsCount = Math.Max(1, (dto.CheckOutDate.Date - dto.CheckInDate.Date).Days);
            dto.NumberOfRooms = dto.HasHotel ? dto.Rooms.Sum(x => x.Count) : 0;
            dto.HotelNightPrice = dto.HasHotel ? dto.Rooms.FirstOrDefault()?.NightPrice ?? 0 : 0;
            dto.HotelTotal = dto.HasHotel ? dto.Rooms.Sum(x => x.Count * x.NightPrice * dto.NightsCount) : 0;
            dto.TotalChildrenCount = dto.ChildrenCountUntil6Years + dto.ChildrenCountUntil12Years;
            dto.SeatsCount = dto.HasTransportation ? dto.TransportationSeats.Count : 0;
            dto.SeatPrice = dto.TransportationSeats.FirstOrDefault()?.SeatPrice ?? 0;
            dto.TransportationTotal = dto.HasTransportation ? dto.TransportationSeats.Sum(x => x.SeatPrice) : 0;
            dto.PaidAmount = dto.Payments.Sum(x => x.Amount);
            dto.GrandTotal = Math.Max(0, dto.HotelTotal + dto.TransportationTotal - dto.Discount);
            dto.RemainingAmount = Math.Max(0, dto.GrandTotal - dto.PaidAmount);
        }

        private static void PrepareDtoForRedisplay(CreateBookingDTO dto)
        {
            dto.PhoneNumbers ??= new List<BookingPhoneDTO>();
            dto.Rooms ??= new List<BookingRoomDTO>();
            dto.Payments ??= new List<BookingPaymentDTO>();
            dto.TransportationSeats ??= new List<BookingSeatDTO>();
            dto.Transportation ??= new TransportationBookingDTO();

            dto.Transportation.DepartureSeats ??= new List<BookingSeatDTO>();
            dto.Transportation.ReturnSeats ??= new List<BookingSeatDTO>();

            if (dto.Rooms.Count == 0)
                dto.Rooms.Add(new BookingRoomDTO { RoomType = RoomType.Double, Count = 1, NightPrice = 0 });

            if (dto.Payments.Count == 0)
                dto.Payments.Add(new BookingPaymentDTO { Amount = 0, PayType = PayType.cache, PaidAt = DateTime.Today });

            if (dto.PhoneNumbers.Count == 0)
                dto.PhoneNumbers.Add(new BookingPhoneDTO { PhoneNumber = string.Empty, Prime = true });
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _bookingManager.GetUpdateDtoAsync(id);
            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BookingCode = string.Empty;
            await FillLookups();
            ViewBag.Name = GetCurrentFullName();
            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateBookingDTO dto)
        {
            ModelState.Clear();

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            var result = await _bookingManager.UpdateAllowedFieldsAsync(dto, userId, User.IsInRole("Admin"));

            if (!result.Success)
            {
                AddErrorsToModelState(result.Errors, result.Message);

                var reload = await _bookingManager.GetUpdateDtoAsync(dto.BookingID);
                var model = reload.Data ?? dto;

                model.HotelName = dto.HotelName;
                model.CheckInDate = dto.CheckInDate;
                model.CheckOutDate = dto.CheckOutDate;
                model.NightsCount = dto.NightsCount;
                model.Rooms = dto.Rooms ?? new List<BookingRoomDTO>();
                model.ChildrenCountUntil6Years = dto.ChildrenCountUntil6Years;
                model.ChildrenCountUntil12Years = dto.ChildrenCountUntil12Years;
                model.TotalChildrenCount = dto.ChildrenCountUntil6Years + dto.ChildrenCountUntil12Years;
                model.Discount = User.IsInRole("Admin") ? dto.Discount : model.Discount;
                model.PhoneNumbers = dto.PhoneNumbers ?? new List<BookingPhoneDTO>();

                model.Transportation ??= new TransportationBookingDTO();
                model.Transportation.TripType = dto.Transportation?.TripType ?? model.Transportation.TripType;
                model.Transportation.DepartureDate = dto.Transportation?.DepartureDate ?? model.Transportation.DepartureDate;
                model.Transportation.ReturnDate = dto.Transportation?.ReturnDate ?? model.Transportation.ReturnDate;

                _bookingManager.EnsureFormDefaults(model);
                await FillLookups();
                ViewBag.Name = GetCurrentFullName();
                ViewBag.BookingCode = reload.Data?.Code ?? string.Empty;

                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Details), new { id = dto.BookingID });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            var result = await _bookingManager.SoftDeleteAsync(id, userId);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpGet]
        public async Task<IActionResult> GetBusLayout(Guid? busId, Guid? tripId, DateTime? date, TripDirection? direction)
        {
            var result = await _bookingManager.GetBusLayoutAsync(busId, tripId, date, direction);
            return Json(new
            {
                success = result.Success,
                message = result.Message,
                errors = result.Errors,
                tripId = result.Data?.TripId,
                busId = result.Data?.BusId,
                busName = result.Data?.BusName,
                plateNumber = result.Data?.PlateNumber,
                rows = result.Data?.Rows,
                columns = result.Data?.Columns,
                layoutJson = result.Data?.LayoutJson,
                seats = result.Data?.Seats ?? new List<BusLayoutSeatResponseDTO>()
            });
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpGet]
        public async Task<IActionResult> GetRouteToLocations(string fromLocation, TripDirection? direction = null)
        {
            var result = await _bookingManager.GetRouteToLocationsAsync(fromLocation, direction);
            return Json(new
            {
                success = result.Success,
                message = result.Message,
                errors = result.Errors,
                data = result.Data ?? new List<string>()
            });
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> GetRouteBuses(string fromLocation, string toLocation, TripDirection? direction = null)
        {
            var result = await _bookingManager.GetRouteBusesAsync(fromLocation, toLocation, direction);
            return Json(new
            {
                success = result.Success,
                message = result.Message,
                errors = result.Errors,
                data = result.Data ?? new List<RouteBusOptionDTO>()
            });
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpGet]
        public async Task<IActionResult> GetRouteBusesBySeatCount(string fromLocation, string toLocation, TripDirection direction, DateTime tripDate, int requiredSeats)
        {
            var result = await _bookingManager.GetRouteBusesBySeatCount(fromLocation, toLocation, direction, tripDate, requiredSeats);
            return Json(new
            {
                success = result.Success,
                message = result.Message,
                errors = result.Errors,
                data = result.Data ?? new List<RouteBusAvailabilityDTO>()
            });
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> FindByCode(string code)
        {
            var r = await _bookingManager.FindByCodeAsync(code);
            return Json(new { success = r.Success, message = r.Message, data = r.Data });
        }
    }
}