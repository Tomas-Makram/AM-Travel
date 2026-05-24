using BusinessLayer.DTOs;
using BusinessLayer.DTOs.Book;
using BusinessLayer.DTOs.Bus;
using BusinessLayer.DTOs.Company;
using BusinessLayer.DTOs.CompanyBookSeat;
using BusinessLayer.DTOs.Trip;
using BusinessLayer.Functions;
using BusinessLayer.Models;
using ClosedXML.Excel;
using DataLayer.Models;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

namespace AM_Travel.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TransportationController : Controller
    {
        private readonly ITransportationManager _manager;
        private readonly ICompanySeatBookingManager _companySeatManager;

        public TransportationController(ITransportationManager manager, ICompanySeatBookingManager companySeatManager)
        {
            _manager = manager;
            _companySeatManager = companySeatManager;
        }

        private string GetCurrentFullName()
        {
            return User.FindFirstValue(ClaimTypes.GivenName) ?? "Unknown";
        }

        [HttpGet]
        public async Task<IActionResult> Companies(string? search, string status = "all")
        {
            ViewBag.Name = GetCurrentFullName();
            ViewBag.Search = search ?? string.Empty;
            ViewBag.Status = status ?? "all";

            var response = await _manager.GetCompanies();

            if (!response.Success || response.Data == null)
            {
                TempData["Error"] = response.Message ?? "Failed to load companies.";
                return View(new List<CompanyDTO>());
            }

            var companies = response.Data;

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();

                companies = companies.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.Name) && x.Name.ToLower().Contains(search)) ||
                    (!string.IsNullOrWhiteSpace(x.PhoneNumber) && x.PhoneNumber.ToLower().Contains(search)))
                    .ToList();
            }

            if (status == "active")
                companies = companies.Where(x => x.IsActive).ToList();

            if (status == "inactive")
                companies = companies.Where(x => !x.IsActive).ToList();

            return View(companies);
        }

        [HttpGet]
        public IActionResult CreateCompany()
        {
            ViewBag.Name = GetCurrentFullName();
            return View(new CreateCompanyDTO());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCompany(CreateCompanyDTO dto)
        {
            ViewBag.Name = GetCurrentFullName();

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var duplicatePhone = await _manager.IsPhoneUsedByAnotherCompany(Guid.Empty, dto.PhoneNumber);

                if (!duplicatePhone.Success)
                    ModelState.AddModelError("", duplicatePhone.Message ?? "Failed to check phone number.");

                if (duplicatePhone.Success && duplicatePhone.Data)
                    ModelState.AddModelError(nameof(dto.PhoneNumber), "This phone number is already registered with another company.");
            }

            if (!ModelState.IsValid)
                return View(dto);

            var result = await _manager.CreateCompany(dto);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Companies));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeCompanyStatus(Guid companyId)
        {
            var result = await _manager.ChangeCompanyStatus(companyId);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(Companies));
        }

        [HttpGet]
        public async Task<IActionResult> EditCompany(Guid id)
        {
            ViewBag.Name = GetCurrentFullName();

            var result = await _manager.GetCompanyForEdit(id);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Company not found.";
                return RedirectToAction(nameof(Companies));
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCompany(UpdateCompanyDTO dto)
        {
            ViewBag.Name = GetCurrentFullName();

            var duplicatePhone = await _manager.IsPhoneUsedByAnotherCompany(dto.CompanyId, dto.PhoneNumber);

            if (!duplicatePhone.Success)
                ModelState.AddModelError("", duplicatePhone.Message ?? "Failed to check phone number.");

            if (duplicatePhone.Success && duplicatePhone.Data)
                ModelState.AddModelError(nameof(dto.PhoneNumber), "This phone number is already registered with another company.");

            if (!ModelState.IsValid)
                return View(dto);

            var result = await _manager.UpdateCompany(dto);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Companies));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCompany(Guid id)
        {
            var result = await _manager.DeleteCompany(id);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(Companies));
        }

        //-----------------------------------------------------------------------//

        [HttpGet]
        public async Task<IActionResult> Buses(string? search, string status = "all", string tripType = "all", string? location = null)
        {
            ViewBag.Search = search ?? string.Empty;
            ViewBag.Status = status ?? "all";
            ViewBag.TripType = tripType ?? "all";
            ViewBag.Location = location ?? string.Empty;
            ViewBag.Name = GetCurrentFullName();

            var response = await _manager.GetBuses();

            if (!response.Success || response.Data == null)
            {
                TempData["Error"] = response.Message ?? "Failed to load buses.";
                ViewBag.Locations = new List<string>();
                return View(new List<BusDTO>());
            }

            var allBuses = response.Data;

            ViewBag.Locations = allBuses
                .SelectMany(x => new[] { x.FromLocation, x.ToLocation })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToList();

            var buses = allBuses;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();

                buses = buses.Where(x =>
                    (!string.IsNullOrWhiteSpace(x.Name) && x.Name.ToLower().Contains(s)) ||
                    (!string.IsNullOrWhiteSpace(x.PlateNumber) && x.PlateNumber.ToLower().Contains(s)))
                    .ToList();
            }

            if (status == "active")
                buses = buses.Where(x => x.IsActive).ToList();

            if (status == "inactive")
                buses = buses.Where(x => !x.IsActive).ToList();

            if (!string.IsNullOrWhiteSpace(location))
            {
                var loc = location.Trim();

                if (tripType == "go")
                {
                    buses = buses.Where(x =>
                        !string.IsNullOrWhiteSpace(x.FromLocation) &&
                        x.FromLocation.Trim().Equals(loc, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else if (tripType == "return")
                {
                    buses = buses.Where(x =>
                        !string.IsNullOrWhiteSpace(x.ToLocation) &&
                        x.ToLocation.Trim().Equals(loc, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    buses = buses.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.FromLocation) &&
                         x.FromLocation.Trim().Equals(loc, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.ToLocation) &&
                         x.ToLocation.Trim().Equals(loc, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
            }

            return View(buses);
        }

        [HttpGet]
        public IActionResult CreateNewBus()
        {
            ViewBag.Name = GetCurrentFullName();

            var defaultLayout = new[]
            {
                new { type = 2, row = 1, column = 1, label = "DRV", hasDoor = false, isActive = true },
                new { type = 7, row = 1, column = 2, label = "", hasDoor = false, isActive = true },
                new { type = 7, row = 1, column = 3, label = "", hasDoor = false, isActive = true },
                new { type = 3, row = 1, column = 4, label = "AST", hasDoor = false, isActive = true },

                new { type = 0, row = 2, column = 1, label = "", hasDoor = false, isActive = true },
                new { type = 0, row = 2, column = 2, label = "", hasDoor = false, isActive = true },
                new { type = 0, row = 2, column = 3, label = "", hasDoor = false, isActive = true },
                new { type = 7, row = 2, column = 4, label = "", hasDoor = true, isActive = true },

                new { type = 0, row = 3, column = 1, label = "", hasDoor = false, isActive = true },
                new { type = 0, row = 3, column = 2, label = "", hasDoor = false, isActive = true },
                new { type = 0, row = 3, column = 4, label = "", hasDoor = true, isActive = true },

                new { type = 0, row = 4, column = 1, label = "", hasDoor = false, isActive = true },
                new { type = 0, row = 4, column = 2, label = "", hasDoor = false, isActive = true },
                new { type = 0, row = 4, column = 4, label = "", hasDoor = false, isActive = true },

                new { type = 0, row = 5, column = 1, label = "", hasDoor = false, isActive = true },
                new { type = 0, row = 5, column = 2, label = "", hasDoor = false, isActive = true },
                new { type = 0, row = 5, column = 3, label = "", hasDoor = false, isActive = true },
                new { type = 0, row = 5, column = 4, label = "", hasDoor = false, isActive = true }
            };
            return View(new CreateBusDTO
            {
                LayoutRows = 5,
                LayoutColumns = 4,
                LayoutJson = System.Text.Json.JsonSerializer.Serialize(defaultLayout)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateNewBus(CreateBusDTO dto)
        {
            ViewBag.Name = GetCurrentFullName();

            if (!ModelState.IsValid)
                return View(dto);

            var result = await _manager.CreateBus(dto);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Buses));
        }

        [HttpGet]
        public async Task<IActionResult> BusDetails(Guid id)
        {
            ViewBag.Name = GetCurrentFullName();

            var result = await _manager.GetBusDetails(id);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Bus not found.";
                return RedirectToAction(nameof(Buses));
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeBusStatus(Guid busId)
        {
            var result = await _manager.ChangeBusStatus(busId);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(BusDetails), new { id = busId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBusLayoutStatus(Guid busId, string layoutJson)
        {
            if (busId == Guid.Empty || string.IsNullOrWhiteSpace(layoutJson))
                return BadRequest(new { success = false, message = "Invalid data." });

            var result = await _manager.UpdateBusLayoutStatus(busId, layoutJson);

            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message, errors = result.Errors });

            return Ok(new { success = true, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> EditBus(Guid id)
        {
            ViewBag.Name = GetCurrentFullName();

            var result = await _manager.GetBusForEdit(id);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Bus not found.";
                return RedirectToAction(nameof(Buses));
            }

            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBus(UpdateBusDTO dto)
        {
            ViewBag.Name = GetCurrentFullName();

            var duplicateName = await _manager.IsBusNameUsedByAnotherBus(dto.BusId, dto.Name);
            if (!duplicateName.Success)
                ModelState.AddModelError("", duplicateName.Message ?? "Failed to check bus name.");

            if (duplicateName.Success && duplicateName.Data)
                ModelState.AddModelError(nameof(dto.Name), "This bus name is already used by another bus.");

            var duplicatePlate = await _manager.IsPlateNumberUsedByAnotherBus(dto.BusId, dto.PlateNumber);
            if (!duplicatePlate.Success)
                ModelState.AddModelError("", duplicatePlate.Message ?? "Failed to check plate number.");

            if (duplicatePlate.Success && duplicatePlate.Data)
                ModelState.AddModelError(nameof(dto.PlateNumber), "This plate number is already used by another bus.");

            if (!ModelState.IsValid)
                return View(dto);

            var result = await _manager.UpdateBus(dto);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return View(dto);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(BusDetails), new { id = dto.BusId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBus(Guid busId)
        {
            var result = await _manager.DeleteBus(busId);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            if (result.Success)
                return RedirectToAction(nameof(Buses));
            else
                return RedirectToAction(nameof(BusDetails), new { id = busId });
        }

        //-----------------------------------------------------------------------//

        [HttpGet]
        public async Task<IActionResult> Trips(DateTime? date)
        {
            ViewBag.Name = GetCurrentFullName();
            ViewBag.Date = date?.ToString("yyyy-MM-dd");

            var search = new TripSearchDTO
            {
                DateFrom = date?.Date,
                DateTo = date?.Date,
                PaymentFilter = CompanyTripPaymentFilter.All
            };

            var result = await _manager.GetTrips(
                companyId: null,
                date: date,
                tripType: TransportationTripType.Departure,
                search: search);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to load trips.";

                return View(new GetTripDTO
                {
                    TripDate = date ?? DateTime.Today,
                    Search = search
                });
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Seats(Guid tripId)
        {
            ViewBag.Name = GetCurrentFullName();

            var result = await _manager.GetTripSeats(tripId);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to load seats.";
                return RedirectToAction(nameof(Trips));
            }

            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> CompanySeatBookingDetails(Guid id)
        {
            var result = await _manager.CompanySeatBookingDetailsAsync(id);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Company seat booking not found.";
                return RedirectToAction(nameof(Trips));
            }
            ViewBag.Name = GetCurrentFullName();
            return View(result.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeSeatStatus(Guid seatId, Guid tripId)
        {
            var result = await _manager.ChangeTripSeatStatus(tripId, seatId);

            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(Seats), new { tripId });
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> DownloadTripSeatsPdf(Guid tripId)
        {
            var result = await _manager.GetTripSeats(tripId);

            if (!result.Success || result.Data == null || !result.Data.Any())
            {
                TempData["Error"] = result.Message ?? "No trip seats found.";
                return RedirectToAction(nameof(Trips));
            }

            QuestPDF.Settings.License = LicenseType.Community;

            var seats = result.Data;
            var firstSeat = seats.First();

            var tripDate = firstSeat.TripDate.ToString("yyyy-MM-dd");
            var routeFrom = Safe(firstSeat.GeneralRouteFrom);
            var routeTo = Safe(firstSeat.GeneralRouteTo);
            var tripRoute = Safe(firstSeat.TripRouteText);

            var busName = Safe(firstSeat.BusName);
            var plateNumber = Safe(firstSeat.PlateNumber);
            var hotelName = Safe(firstSeat.HotelName);

            var totalSeats = seats.Count(x =>
                x.IsActive &&
                (x.SeatType == SeatType.Normal || x.SeatType == SeatType.VIP));

            var reservedSeats = seats.Count(x => x.IsReserved);
            var availableSeats = Math.Max(0, totalSeats - reservedSeats);

            var reservedGroups = seats
                .Where(x => x.IsReserved)
                .GroupBy(x => x.IsCompanyBooking
                    ? $"C_{(string.IsNullOrWhiteSpace(x.CompanyBookingGroupKey) ? x.CompanySeatBookingId.ToString() : x.CompanyBookingGroupKey)}"
                    : $"B_{x.BookingId}")
                .Select(g => new
                {
                    First = g.First(),
                    Seats = g.OrderBy(x => x.RowNumber).ThenBy(x => x.ColumnNumber).ToList()
                })
                .OrderBy(x => x.First.IsCompanyBooking)
                .ThenBy(x => x.First.CompanyName)
                .ThenBy(x => x.First.ReservedByClient)
                .ToList();

            string Safe(string? value) =>
                string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

            string SeatText(IEnumerable<BusinessLayer.DTOs.Trip.TripSeatStatusDTO> groupSeats) =>
                string.Join(", ", groupSeats.Select(x =>
                    !string.IsNullOrWhiteSpace(x.SeatLabel)
                        ? x.SeatLabel
                        : x.SeatNumber.ToString()));

            string FixRoute(string? from, string? to)
            {
                return $"{Safe(from)} ← {Safe(to)}";
            }

            string BookingType(BusinessLayer.DTOs.Trip.TripSeatStatusDTO b)
            {
                if (b.IsCompanyBooking) return "Company";
                if (b.HasHotel && b.HasTransportation) return "Hotel + Transport";
                if (b.HasHotel) return "Hotel";
                return "Transport";
            }

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(25);
                    page.PageColor(Colors.White);

                    page.DefaultTextStyle(x => x
                        .FontFamily("Arial")
                        .FontSize(9)
                        .FontColor("#172033"));

                    page.Header().Column(header =>
                    {
                        header.Item()
                            .Background("#ff7a18")
                            .Padding(18)
                            .Column(mainCol =>
                            {
                                mainCol.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(col =>
                                    {
                                        col.Item().Text("AM Travel - Trip Seats Report")
                                            .FontSize(26)
                                            .Bold()
                                            .FontColor(Colors.White);

                                        col.Item().PaddingTop(5)
                                            .Text("Passengers, seats, routes and booking details")
                                            .FontSize(11)
                                            .FontColor("#fff7ed");
                                    });

                                    row.ConstantItem(220)
                                        .AlignRight()
                                        .Column(col =>
                                        {
                                            col.Item().AlignRight()
                                                .Text($"Date : {tripDate}")
                                                .FontSize(13)
                                                .Bold()
                                                .FontColor(Colors.White);

                                            col.Item().PaddingTop(5)
                                                .AlignRight()
                                                .Text($"Trip Type : {tripRoute}")
                                                .FontSize(13)
                                                .Bold()
                                                .FontColor(Colors.White);
                                        });
                                });

                                mainCol.Item().PaddingTop(14);

                                mainCol.Item()
                                    .Background("#fff7ed")
                                    .Border(1)
                                    .BorderColor("#ffd7b0")
                                    .Padding(14)
                                    .Row(row =>
                                    {
                                        row.RelativeItem().Column(col =>
                                        {
                                            col.Item().Text("Route")
                                                .FontSize(10)
                                                .Bold()
                                                .FontColor("#667085");

                                            col.Item().PaddingTop(2)
                                                .Text(FixRoute(routeFrom, routeTo))
                                                .FontSize(15)
                                                .Bold()
                                                .FontColor("#172033");
                                        });

                                        row.RelativeItem().Column(col =>
                                        {
                                            col.Item().Text("Hotel Name")
                                                .FontSize(10)
                                                .Bold()
                                                .FontColor("#667085");

                                            col.Item().PaddingTop(2)
                                                .Text(hotelName)
                                                .FontSize(15)
                                                .Bold()
                                                .FontColor("#172033");
                                        });

                                        row.RelativeItem().Column(col =>
                                        {
                                            col.Item().Text("Bus Name")
                                                .FontSize(10)
                                                .Bold()
                                                .FontColor("#667085");

                                            col.Item().PaddingTop(2)
                                                .Text(busName)
                                                .FontSize(15)
                                                .Bold()
                                                .FontColor("#172033");
                                        });

                                        row.RelativeItem().Column(col =>
                                        {
                                            col.Item().Text("Plate Number")
                                                .FontSize(10)
                                                .Bold()
                                                .FontColor("#667085");

                                            col.Item().PaddingTop(2)
                                                .Text(plateNumber)
                                                .FontSize(15)
                                                .Bold()
                                                .FontColor("#172033");
                                        });

                                        row.ConstantItem(120)
                                            .Background("#ffffff")
                                            .Border(1)
                                            .BorderColor("#ffd7b0")
                                            .Padding(10)
                                            .Column(col =>
                                            {
                                                col.Item().AlignCenter().Text("Total")
                                                    .FontSize(10)
                                                    .Bold()
                                                    .FontColor("#667085");

                                                col.Item().AlignCenter().PaddingTop(2)
                                                    .Text(totalSeats.ToString())
                                                    .FontSize(22)
                                                    .Bold()
                                                    .FontColor("#ff7a18");
                                            });

                                        row.ConstantItem(120)
                                            .Background("#ffffff")
                                            .Border(1)
                                            .BorderColor("#ffd7b0")
                                            .Padding(10)
                                            .Column(col =>
                                            {
                                                col.Item().AlignCenter().Text("Reserved")
                                                    .FontSize(10)
                                                    .Bold()
                                                    .FontColor("#667085");

                                                col.Item().AlignCenter().PaddingTop(2)
                                                    .Text(reservedSeats.ToString())
                                                    .FontSize(22)
                                                    .Bold()
                                                    .FontColor("#172033");
                                            });

                                        row.ConstantItem(120)
                                            .Background("#ffffff")
                                            .Border(1)
                                            .BorderColor("#ffd7b0")
                                            .Padding(10)
                                            .Column(col =>
                                            {
                                                col.Item().AlignCenter().Text("Available")
                                                    .FontSize(10)
                                                    .Bold()
                                                    .FontColor("#667085");

                                                col.Item().AlignCenter().PaddingTop(2)
                                                    .Text(availableSeats.ToString())
                                                    .FontSize(22)
                                                    .Bold()
                                                    .FontColor("#079455");
                                            });
                                    });
                            });
                    });

                    page.Content().PaddingTop(14).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(.7f);
                            columns.RelativeColumn(.9f);
                            columns.RelativeColumn(.8f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(.8f);
                            columns.RelativeColumn(.8f);
                            columns.RelativeColumn(.9f);
                        });

                        table.Header(header =>
                        {
                            string[] headers =
                            {
                        "Client Name",
                        "Phones",
                        "Seats",
                        "Type",
                        "Trip",
                        "Route",
                        "Company",
                        "Total",
                        "Paid",
                        "Remaining"
                    };

                            foreach (var h in headers)
                            {
                                header.Cell()
                                    .Background("#ff7a18")
                                    .Border(1)
                                    .BorderColor("#ff7a18")
                                    .PaddingVertical(7)
                                    .PaddingHorizontal(5)
                                    .AlignCenter()
                                    .Text(h)
                                    .Bold()
                                    .FontColor(Colors.White)
                                    .FontSize(9);
                            }
                        });

                        var rowIndex = 0;

                        foreach (var item in reservedGroups)
                        {
                            rowIndex++;

                            var b = item.First;
                            var bg = rowIndex % 2 == 0 ? "#fffaf5" : "#ffffff";

                            var route = FixRoute(b.SeatRouteFrom, b.SeatRouteTo);

                            var total = b.IsCompanyBooking
                                ? item.Seats.Sum(x => x.GrandTotal)
                                : b.GrandTotal;

                            var paid = b.PaidAmount;

                            var remaining = b.IsCompanyBooking
                                ? item.Seats.Sum(x => x.RemainingAmount)
                                : b.RemainingAmount;

                            void Cell(string? text, bool center = false, string? color = null)
                            {
                                var cell = table.Cell()
                                    .Background(bg)
                                    .BorderBottom(1)
                                    .BorderColor("#f0d8c2")
                                    .PaddingVertical(6)
                                    .PaddingHorizontal(5);

                                if (center)
                                {
                                    cell.AlignCenter()
                                        .Text(Safe(text))
                                        .FontSize(8)
                                        .FontColor(color ?? "#172033");
                                }
                                else
                                {
                                    cell.Text(Safe(text))
                                        .FontSize(8)
                                        .FontColor(color ?? "#172033");
                                }
                            }

                            Cell(b.ReservedByClient);
                            Cell(b.PhoneNumbersText);
                            Cell(SeatText(item.Seats), true);
                            Cell(BookingType(b), true);
                            Cell(b.TripRouteText, true);
                            Cell(route);
                            Cell(b.CompanyName);
                            Cell(total.ToString("N2"), true);
                            Cell(paid.ToString("N2"), true, "#079455");
                            Cell(remaining.ToString("N2"), true, remaining > 0 ? "#dc2626" : "#079455");
                        }
                    });

                    page.Footer()
                        .PaddingTop(10)
                        .BorderTop(1)
                        .BorderColor("#f0d8c2")
                        .Row(row =>
                        {
                            row.RelativeItem()
                                .Text($"Generated at {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(8)
                                .FontColor("#667085");

                            row.ConstantItem(120)
                                .AlignRight()
                                .Text(x =>
                                {
                                    x.Span("Page ").FontSize(8).FontColor("#667085");
                                    x.CurrentPageNumber().FontSize(8).FontColor("#667085");
                                    x.Span(" / ").FontSize(8).FontColor("#667085");
                                    x.TotalPages().FontSize(8).FontColor("#667085");
                                });
                        });
                });
            }).GeneratePdf();

            var fileName = $"AM-Travel-Trip-Seats-{tripDate}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        //-----------------------------------------------------------------------//

        private static string GetValidationMessage(IEnumerable<ValidationResult> errors)
        {
            return string.Join(" ", errors
                .Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> Index(ConnectCompanyWithTripsDTO dto)
        {
            var selectedCompanyId = dto.CompanyId == Guid.Empty ? (Guid?)null : dto.CompanyId;
            var selectedDate = dto.TripDate == default ? DateTime.Today : dto.TripDate.Date;
            var selectedTripType = Enum.IsDefined(typeof(TransportationTripType), dto.TripType)
                ? dto.TripType
                : TransportationTripType.Departure;

            var search = new TripSearchDTO
            {
                CompanyId = selectedCompanyId,
                DateFrom = null,
                DateTo = null,
                Location = null,
                TripType = null,
                PaymentFilter = CompanyTripPaymentFilter.All
            };

            var result = await _manager.GetTrips(selectedCompanyId, selectedDate, selectedTripType, search);

            ViewBag.Name = GetCurrentFullName();
            ViewBag.SelectedCompanyId = selectedCompanyId;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.SelectedTripType = selectedTripType;
            ViewBag.SelectedDepartureTripId = dto.DepartureTripId;
            ViewBag.SelectedReturnTripId = dto.ReturnTripId;
            ViewBag.AvailableTrips = new List<AvailableTripListDTO>();
            ViewBag.LinkedTrips = new List<AvailableTripListDTO>();

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to load page.";
                return View(new GetTripDTO
                {
                    CompanyId = selectedCompanyId,
                    TripDate = selectedDate,
                    TripType = selectedTripType,
                    Search = search
                });
            }

            dto.TripDate = selectedDate;
            dto.TripType = selectedTripType;

            if (!dto.ValidateForSearch().Any())
            {
                var availableResult = await _manager.GetAvailableTripsAsync(selectedDate, selectedTripType, selectedCompanyId);
                ViewBag.AvailableTrips = availableResult.Success
                    ? availableResult.Data ?? new List<AvailableTripListDTO>()
                    : new List<AvailableTripListDTO>();

                var linkedResult = await _manager.GetTripsConnectedByCompanyAsync(selectedCompanyId!.Value, selectedDate);
                ViewBag.LinkedTrips = linkedResult.Success
                    ? linkedResult.Data ?? new List<AvailableTripListDTO>()
                    : new List<AvailableTripListDTO>();
            }

            result.Data.CompanyId = selectedCompanyId;
            result.Data.TripDate = selectedDate;
            result.Data.TripType = selectedTripType;
            result.Data.Search = search;
            
            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public IActionResult GetAvailableTrips(ConnectCompanyWithTripsDTO dto)
        {
            var errors = dto.ValidateForSearch().ToList();

            if (errors.Any())
                TempData["Error"] = GetValidationMessage(errors);

            return RedirectToAction(nameof(Index), new
            {
                CompanyId = dto.CompanyId,
                TripDate = dto.TripDate == default ? DateTime.Today.ToString("yyyy-MM-dd") : dto.TripDate.ToString("yyyy-MM-dd"),
                TripType = dto.TripType,
                DepartureTripId = dto.DepartureTripId,
                ReturnTripId = dto.ReturnTripId
            });
        }

        [Authorize(Roles = "Admin,Viewer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConnectTripByCompany(ConnectCompanyWithTripsDTO dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" ", ModelState.Values
                    .SelectMany(x => x.Errors)
                    .Select(x => x.ErrorMessage)
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

                return RedirectToAction(nameof(Index), new
                {
                    CompanyId = dto.CompanyId,
                    TripDate = dto.TripDate == default ? DateTime.Today.ToString("yyyy-MM-dd") : dto.TripDate.ToString("yyyy-MM-dd"),
                    TripType = dto.TripType,
                    DepartureTripId = dto.DepartureTripId,
                    ReturnTripId = dto.ReturnTripId
                });
            }

            var result = await _manager.ConnectTripByCompanyAsync(dto);
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            return RedirectToAction(nameof(Index), new
            {
                CompanyId = dto.CompanyId,
                TripDate = dto.TripDate.ToString("yyyy-MM-dd"),
                TripType = dto.TripType,
                DepartureTripId = dto.DepartureTripId,
                ReturnTripId = dto.ReturnTripId
            });
        }

        [Authorize(Roles = "Admin,Viewer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConnectTripByCompany(ConnectCompanyWithTripsDTO dto)
        {
            var errors = dto.ValidateForDelete().ToList();

            if (errors.Any())
            {
                TempData["Error"] = GetValidationMessage(errors);
            }
            else
            {
                var result = await _manager.DeleteConnectTripByCompanyAsync(
                    dto.CompanyId,
                    dto.TripId!.Value,
                    User.IsInRole("Admin"));

                TempData[result.Success ? "Success" : "Error"] = result.Message;
            }

            return RedirectToAction(nameof(Index), new
            {
                CompanyId = dto.CompanyId,
                TripDate = dto.TripDate == default ? DateTime.Today.ToString("yyyy-MM-dd") : dto.TripDate.ToString("yyyy-MM-dd"),
                TripType = dto.TripType,
                DepartureTripId = dto.DepartureTripId,
                ReturnTripId = dto.ReturnTripId
            });
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public IActionResult GetTripsConnectedByCompany(ConnectCompanyWithTripsDTO dto)
        {
            var errors = dto.ValidateForSearch().ToList();

            if (errors.Any())
                TempData["Error"] = GetValidationMessage(errors);

            return RedirectToAction(nameof(Index), new
            {
                CompanyId = dto.CompanyId,
                TripDate = dto.TripDate == default ? DateTime.Today.ToString("yyyy-MM-dd") : dto.TripDate.ToString("yyyy-MM-dd"),
                TripType = dto.TripType,
                DepartureTripId = dto.DepartureTripId,
                ReturnTripId = dto.ReturnTripId
            });
        }

        //-----------------------------------------------------------------------//

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> Accounting(TripSearchDTO search)
        {
            search ??= new TripSearchDTO();

            var result = await _manager.GetCompanyTripAccountingByCoumpanyAsync(search);

            ViewBag.Name = GetCurrentFullName();

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to load accounting page.";
                return View(new GetTripDTO { Search = search });
            }

            result.Data.Search = search;
            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Viewer")]
        [HttpGet]
        public IActionResult GetCompanyTripAccounting(TripSearchDTO search)
        {
            return RedirectToAction(nameof(Accounting), new
            {
                search.CompanyId,
                search.DateFrom,
                search.DateTo,
                search.Location,
                search.TripType,
                search.PaymentFilter
            });
        }

        [Authorize(Roles = "Admin,Viewer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCompanyTripPrice(Guid companyId, Guid tripId, decimal price, TripSearchDTO search)
        {
            var result = await _manager.UpdateCompanyTripPriceAsync(companyId, tripId, price);

            TempData[result.Success ? "Success" : "Error"] =
                result.Message ?? (result.Success ? "Price updated successfully." : "Failed to update price.");

            return RedirectToAction(nameof(GetCompanyTripAccounting), search);
        }

        [Authorize(Roles = "Admin,Viewer,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCompanyTripPayment(Guid companyId, Guid tripId, decimal amount, TripSearchDTO search)
        {
            var result = await _manager.AddCompanyTripPaymentAsync(companyId, tripId, amount);

            TempData[result.Success ? "Success" : "Error"] =
                result.Message ?? (result.Success ? "Payment added successfully." : "Failed to add payment.");

            return RedirectToAction(nameof(GetCompanyTripAccounting), search);
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> ExportAccountingExcel(TripSearchDTO search, bool download = false)
        {
            if (!download)
            {
                var url = Url.Action(nameof(ExportAccountingExcel), "Transportation", new
                {
                    search.CompanyId,
                    search.DateFrom,
                    search.DateTo,
                    search.Location,
                    search.TripType,
                    search.PaymentFilter,
                    download = true
                });

                return Content($@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8' /><title>Downloading...</title></head>
<body>
<p style='font-family:Arial'>Downloading Excel file...</p>
<script>
    const a = document.createElement('a');
    a.href = '{url}';
    a.download = '';
    document.body.appendChild(a);
    a.click();
    setTimeout(function () {{ window.close(); }}, 1500);
</script>
</body>
</html>", "text/html");
            }

            var result = await _manager.GetCompanyTripAccountingAsync(search);
            if (!result.Success)
                return BadRequest(result.Message);

            var trips = result.Data ?? new List<AvailableTripListDTO>();

            var groups = trips
                .GroupBy(x => x.CompanyTripGroupId.HasValue ? $"round-{x.CompanyTripGroupId.Value}" : $"single-{x.TripId}")
                .Select(g => new
                {
                    IsRound = g.Any(x => x.CompanyTripGroupId.HasValue) && g.Count() > 1,
                    Items = g.OrderBy(x => x.Direction).ToList(),
                    First = g.OrderBy(x => x.Direction).First(),
                    TotalSeats = g.Sum(x => x.TotalSeats),
                    ReservedSeats = g.Sum(x => x.ReservedSeats),
                    AvailableSeats = g.Sum(x => x.AvailableSeats),
                    Price = g.Sum(x => x.CompanyTripPrice),
                    Paid = g.Sum(x => x.CompanyTripPaidAmount),
                    Remaining = g.Sum(x => x.CompanyTripRemainingAmount)
                })
                .ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Company Accounting");

            var orange = XLColor.FromHtml("#FF7A18");
            var darkBlue = XLColor.FromHtml("#172033");
            var lightOrange = XLColor.FromHtml("#FFF3E8");
            var green = XLColor.FromHtml("#16A34A");
            var red = XLColor.FromHtml("#DC2626");
            var white = XLColor.White;

            sheet.Range("A1:L1").Merge();
            sheet.Cell("A1").Value = "AM Travel - Company Trips Accounting";
            sheet.Cell("A1").Style.Font.Bold = true;
            sheet.Cell("A1").Style.Font.FontSize = 18;
            sheet.Cell("A1").Style.Font.FontColor = white;
            sheet.Cell("A1").Style.Fill.BackgroundColor = orange;
            sheet.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            sheet.Range("A2:L2").Merge();
            sheet.Cell("A2").Value = $"Generated At: {DateTime.Now:yyyy-MM-dd HH:mm}";
            sheet.Cell("A2").Style.Font.Bold = true;
            sheet.Cell("A2").Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE5CC");

            var headerRow = 4;
            string[] headers =
            {
        "Date", "Company", "Phone", "Type", "Bus / Plate", "Location",
        "Total Seats", "Reserved", "Available", "Price", "Paid", "Remaining"
    };

            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = white;
                cell.Style.Fill.BackgroundColor = darkBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            var row = headerRow + 1;

            foreach (var group in groups)
            {
                if (group.IsRound)
                {
                    var first = group.First;

                    sheet.Cell(row, 1).Value = first.TripDate.ToString("dd/MM/yyyy");
                    sheet.Cell(row, 2).Value = first.CompanyName;
                    sheet.Cell(row, 3).Value = first.CompanyPhoneNumber;
                    sheet.Cell(row, 4).Value = "Round";

                    sheet.Cell(row, 5).Value = string.Join(Environment.NewLine, group.Items.Select(x =>
                        $"{(x.DirectionText ?? x.Direction.ToString())}: {(string.IsNullOrWhiteSpace(x.BusName) ? "-" : x.BusName)} | Plate: {(string.IsNullOrWhiteSpace(x.PlateNumber) ? "-" : x.PlateNumber)}"));

                    sheet.Cell(row, 6).Value = string.Join(Environment.NewLine, group.Items.Select(x =>
                        $"{(x.DirectionText ?? x.Direction.ToString())}: {x.FromLocation} -> {x.ToLocation}"));

                    sheet.Cell(row, 7).Value = string.Join(Environment.NewLine, group.Items.Select(x =>
                        $"{(x.DirectionText ?? x.Direction.ToString())}: {x.TotalSeats}")) + Environment.NewLine + $"Total: {group.TotalSeats}";

                    sheet.Cell(row, 8).Value = string.Join(Environment.NewLine, group.Items.Select(x =>
                        $"{(x.DirectionText ?? x.Direction.ToString())}: {x.ReservedSeats}")) + Environment.NewLine + $"Total: {group.ReservedSeats}";

                    sheet.Cell(row, 9).Value = string.Join(Environment.NewLine, group.Items.Select(x =>
                        $"{(x.DirectionText ?? x.Direction.ToString())}: {x.AvailableSeats}")) + Environment.NewLine + $"Total: {group.AvailableSeats}";

                    sheet.Cell(row, 10).Value = string.Join(Environment.NewLine, group.Items.Select(x =>
                        $"{(x.DirectionText ?? x.Direction.ToString())}: {x.CompanyTripPrice:N2}")) + Environment.NewLine + $"Total: {group.Price:N2}";

                    sheet.Cell(row, 11).Value = string.Join(Environment.NewLine, group.Items.Select(x =>
                        $"{(x.DirectionText ?? x.Direction.ToString())}: {x.CompanyTripPaidAmount:N2}")) + Environment.NewLine + $"Total: {group.Paid:N2}";

                    sheet.Cell(row, 12).Value = group.Remaining;

                    sheet.Row(row).Style.Fill.BackgroundColor = lightOrange;
                    sheet.Cell(row, 4).Style.Font.FontColor = orange;
                    sheet.Cell(row, 4).Style.Font.Bold = true;
                }
                else
                {
                    var item = group.First;

                    sheet.Cell(row, 1).Value = item.TripDate.ToString("dd/MM/yyyy");
                    sheet.Cell(row, 2).Value = item.CompanyName;
                    sheet.Cell(row, 3).Value = item.CompanyPhoneNumber;
                    sheet.Cell(row, 4).Value = item.DirectionText ?? item.Direction.ToString();
                    sheet.Cell(row, 5).Value = $"{(string.IsNullOrWhiteSpace(item.BusName) ? "-" : item.BusName)}{Environment.NewLine}Plate: {(string.IsNullOrWhiteSpace(item.PlateNumber) ? "-" : item.PlateNumber)}";
                    sheet.Cell(row, 6).Value = $"{item.FromLocation} -> {item.ToLocation}";
                    sheet.Cell(row, 7).Value = item.TotalSeats;
                    sheet.Cell(row, 8).Value = item.ReservedSeats;
                    sheet.Cell(row, 9).Value = item.AvailableSeats;
                    sheet.Cell(row, 10).Value = item.CompanyTripPrice;
                    sheet.Cell(row, 11).Value = item.CompanyTripPaidAmount;
                    sheet.Cell(row, 12).Value = item.CompanyTripRemainingAmount;
                }

                sheet.Range(row, 1, row, 12).Style.Alignment.WrapText = true;
                sheet.Range(row, 1, row, 12).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Range(row, 1, row, 12).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                sheet.Range(row, 1, row, 12).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                sheet.Cell(row, 10).Style.Font.Bold = true;
                sheet.Cell(row, 11).Style.Font.FontColor = green;
                sheet.Cell(row, 11).Style.Font.Bold = true;

                var remainingValue = group.Remaining;
                sheet.Cell(row, 12).Style.Font.Bold = true;
                sheet.Cell(row, 12).Style.Font.FontColor = remainingValue > 0 ? red : green;

                row++;
            }

            row++;

            sheet.Cell(row, 9).Value = "Grand Totals";
            sheet.Cell(row, 10).Value = trips.Sum(x => x.CompanyTripPrice);
            sheet.Cell(row, 11).Value = trips.Sum(x => x.CompanyTripPaidAmount);
            sheet.Cell(row, 12).Value = trips.Sum(x => x.CompanyTripRemainingAmount);

            sheet.Range(row, 9, row, 12).Style.Font.Bold = true;
            sheet.Range(row, 9, row, 12).Style.Fill.BackgroundColor = orange;
            sheet.Range(row, 9, row, 12).Style.Font.FontColor = white;
            sheet.Range(row, 9, row, 12).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            sheet.Columns().AdjustToContents();
            sheet.Rows().AdjustToContents();

            sheet.Column(5).Width = 35;
            sheet.Column(6).Width = 35;
            sheet.Column(7).Width = 18;
            sheet.Column(8).Width = 18;
            sheet.Column(9).Width = 18;
            sheet.Column(10).Width = 22;
            sheet.Column(11).Width = 22;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CompanyTripsAccounting_{DateTime.Now:yyyyMMddHHmm}.xlsx");
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> ExportAccountingPdf(TripSearchDTO search, bool download = false)
        {
            if (!download)
            {
                var url = Url.Action(nameof(ExportAccountingPdf), "Transportation", new
                {
                    search.CompanyId,
                    search.DateFrom,
                    search.DateTo,
                    search.Location,
                    search.TripType,
                    search.PaymentFilter,
                    download = true
                });

                return Content($@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8' /><title>Downloading...</title></head>
<body>
<p style='font-family:Arial'>Downloading PDF file...</p>
<script>
    const a = document.createElement('a');
    a.href = '{url}';
    a.download = '';
    document.body.appendChild(a);
    a.click();
    setTimeout(function () {{ window.close(); }}, 1500);
</script>
</body>
</html>", "text/html");
            }

            QuestPDF.Settings.License = LicenseType.Community;

            var result = await _manager.GetCompanyTripAccountingAsync(search);
            if (!result.Success)
                return BadRequest(result.Message);

            var trips = result.Data ?? new List<AvailableTripListDTO>();

            var groups = trips
                .GroupBy(x => x.CompanyTripGroupId.HasValue ? $"round-{x.CompanyTripGroupId.Value}" : $"single-{x.TripId}")
                .Select(g => new
                {
                    IsRound = g.Any(x => x.CompanyTripGroupId.HasValue) && g.Count() > 1,
                    Items = g.OrderBy(x => x.Direction).ToList(),
                    First = g.OrderBy(x => x.Direction).First(),
                    TotalSeats = g.Sum(x => x.TotalSeats),
                    ReservedSeats = g.Sum(x => x.ReservedSeats),
                    AvailableSeats = g.Sum(x => x.AvailableSeats),
                    Price = g.Sum(x => x.CompanyTripPrice),
                    Paid = g.Sum(x => x.CompanyTripPaidAmount),
                    Remaining = g.Sum(x => x.CompanyTripRemainingAmount)
                })
                .ToList();

            var totalPrice = trips.Sum(x => x.CompanyTripPrice);
            var totalPaid = trips.Sum(x => x.CompanyTripPaidAmount);
            var totalRemaining = trips.Sum(x => x.CompanyTripRemainingAmount);

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(14);
                    page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item()
                            .Background("#FF7A18")
                            .Padding(10)
                            .Column(header =>
                            {
                                header.Item().Text("AM Travel - Company Trips Accounting")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.White);

                                header.Item().Text($"Generated At: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                    .FontSize(9)
                                    .FontColor(Colors.White);

                                if (search.CompanyId.HasValue)
                                {
                                    var selectedCompany = trips.FirstOrDefault()?.CompanyName ?? "";
                                    if (!string.IsNullOrWhiteSpace(selectedCompany))
                                        header.Item().Text($"Company: {selectedCompany}")
                                            .FontSize(10)
                                            .Bold()
                                            .FontColor(Colors.White);
                                }
                            });

                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Background("#172033").Padding(6).Text($"Total Price: {totalPrice:N2}").FontColor(Colors.White).Bold();
                            row.RelativeItem().Background("#16A34A").Padding(6).Text($"Total Paid: {totalPaid:N2}").FontColor(Colors.White).Bold();
                            row.RelativeItem().Background(totalRemaining > 0 ? "#DC2626" : "#16A34A").Padding(6).Text($"Total Remaining: {totalRemaining:N2}").FontColor(Colors.White).Bold();
                        });
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.8f);  // Date
                            columns.RelativeColumn(1.25f); // Company
                            columns.RelativeColumn(1.1f);  // Phone
                            columns.RelativeColumn(0.75f); // Type
                            columns.RelativeColumn(1.8f);  // Bus
                            columns.RelativeColumn(1.9f);  // Location
                            columns.RelativeColumn(0.8f);  // Total
                            columns.RelativeColumn(0.9f);  // Reserved
                            columns.RelativeColumn(0.9f);  // Available
                            columns.RelativeColumn(1.05f); // Price
                            columns.RelativeColumn(1.05f); // Paid
                            columns.RelativeColumn(1.05f); // Remaining
                        });

                        string[] headers =
                        {
                    "Date", "Company", "Phone", "Type", "Bus / Plate", "Location",
                    "Total", "Reserved", "Available", "Price", "Paid", "Remaining"
                };

                        table.Header(header =>
                        {
                            foreach (var h in headers)
                            {
                                header.Cell()
                                    .Background("#172033")
                                    .Border(0.5f)
                                    .BorderColor("#2D3A52")
                                    .Padding(4)
                                    .Text(h)
                                    .FontColor(Colors.White)
                                    .Bold();
                            }
                        });

                        foreach (var group in groups)
                        {
                            if (group.IsRound)
                            {
                                var first = group.First;

                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Text(first.TripDate.ToString("dd/MM/yyyy")).Bold();
                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Text(first.CompanyName).Bold();
                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Text(first.CompanyPhoneNumber);
                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Text("Round").FontColor("#FF7A18").Bold();

                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Column(c =>
                                {
                                    foreach (var trip in group.Items)
                                    {
                                        c.Item().Text($"{trip.DirectionText ?? trip.Direction.ToString()}:").FontColor("#FF7A18").Bold();
                                        c.Item().Text(string.IsNullOrWhiteSpace(trip.BusName) ? "-" : trip.BusName).Bold();
                                        c.Item().Text($"Plate: {(string.IsNullOrWhiteSpace(trip.PlateNumber) ? "-" : trip.PlateNumber)}");
                                        c.Item().PaddingBottom(3);
                                    }
                                });

                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Column(c =>
                                {
                                    foreach (var trip in group.Items)
                                    {
                                        c.Item().Text($"{trip.DirectionText ?? trip.Direction.ToString()}:").FontColor("#FF7A18").Bold();
                                        c.Item().Text($"{trip.FromLocation} -> {trip.ToLocation}");
                                        c.Item().PaddingBottom(3);
                                    }
                                });

                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Column(c =>
                                {
                                    foreach (var trip in group.Items)
                                        c.Item().Text($"{trip.DirectionText ?? trip.Direction.ToString()}: {trip.TotalSeats}");
                                    c.Item().Text($"Total: {group.TotalSeats}").FontColor("#FF7A18").Bold();
                                });

                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Column(c =>
                                {
                                    foreach (var trip in group.Items)
                                        c.Item().Text($"{trip.DirectionText ?? trip.Direction.ToString()}: {trip.ReservedSeats}");
                                    c.Item().Text($"Total: {group.ReservedSeats}").FontColor("#FF7A18").Bold();
                                });

                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Column(c =>
                                {
                                    foreach (var trip in group.Items)
                                        c.Item().Text($"{trip.DirectionText ?? trip.Direction.ToString()}: {trip.AvailableSeats}");
                                    c.Item().Text($"Total: {group.AvailableSeats}").FontColor("#FF7A18").Bold();
                                });

                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Column(c =>
                                {
                                    foreach (var trip in group.Items)
                                        c.Item().Text($"{trip.DirectionText ?? trip.Direction.ToString()}: {trip.CompanyTripPrice:N2}");
                                    c.Item().Text($"Total: {group.Price:N2}").FontColor("#FF7A18").Bold();
                                });

                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4).Column(c =>
                                {
                                    foreach (var trip in group.Items)
                                        c.Item().Text($"{trip.DirectionText ?? trip.Direction.ToString()}: {trip.CompanyTripPaidAmount:N2}").FontColor("#16A34A");
                                    c.Item().Text($"Total: {group.Paid:N2}").FontColor("#16A34A").Bold();
                                });

                                table.Cell().Background("#FFF3E8").Border(0.5f).Padding(4)
                                    .Text(group.Remaining.ToString("N2"))
                                    .FontColor(group.Remaining > 0 ? "#DC2626" : "#16A34A")
                                    .Bold();
                            }
                            else
                            {
                                var item = group.First;

                                table.Cell().Border(0.5f).Padding(4).Text(item.TripDate.ToString("dd/MM/yyyy"));
                                table.Cell().Border(0.5f).Padding(4).Text(item.CompanyName);
                                table.Cell().Border(0.5f).Padding(4).Text(item.CompanyPhoneNumber);
                                table.Cell().Border(0.5f).Padding(4).Text(item.DirectionText ?? item.Direction.ToString());

                                table.Cell().Border(0.5f).Padding(4).Column(c =>
                                {
                                    c.Item().Text(string.IsNullOrWhiteSpace(item.BusName) ? "-" : item.BusName).Bold();
                                    c.Item().Text($"Plate: {(string.IsNullOrWhiteSpace(item.PlateNumber) ? "-" : item.PlateNumber)}");
                                });

                                table.Cell().Border(0.5f).Padding(4).Text($"{item.FromLocation} -> {item.ToLocation}");
                                table.Cell().Border(0.5f).Padding(4).Text(item.TotalSeats.ToString());
                                table.Cell().Border(0.5f).Padding(4).Text(item.ReservedSeats.ToString());
                                table.Cell().Border(0.5f).Padding(4).Text(item.AvailableSeats.ToString());
                                table.Cell().Border(0.5f).Padding(4).Text(item.CompanyTripPrice.ToString("N2")).Bold();
                                table.Cell().Border(0.5f).Padding(4).Text(item.CompanyTripPaidAmount.ToString("N2")).FontColor("#16A34A").Bold();

                                table.Cell().Border(0.5f).Padding(4)
                                    .Text(item.CompanyTripRemainingAmount.ToString("N2"))
                                    .FontColor(item.CompanyTripRemainingAmount > 0 ? "#DC2626" : "#16A34A")
                                    .Bold();
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated by AM Travel - ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });
                });
            }).GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"CompanyTripsAccounting_{DateTime.Now:yyyyMMddHHmm}.pdf");
        }

        //------------------------------------------------------------------------------//

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> SeatBookingBound(SeatBookingBoundPageDTO dto)
        {
            ViewBag.Name = GetCurrentFullName();
            var result = await _companySeatManager.GetSeatBookingBoundPageAsync(dto);
            
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ",
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                return View(nameof(SeatBookingBound), dto);
            }

            if (!result.Success || result.Data == null)
            {
                ViewBag.ErrorMessage = result.Message ?? "Page failed to load.";

                return View(new SeatBookingBoundPageDTO());
            }

            ViewBag.SuccessMessage = TempData["Success"] as string;
            ViewBag.ErrorMessage = TempData["Error"] as string ?? result.Message;

            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeatBookingBoundBook(CreateBookingSeatCompanyDTO dto)
        {
            object routeValues = new
            {
                tripDate = dto.TripDate.ToString("yyyy-MM-dd"),
                direction = dto.Direction,
                routeFrom = dto.FromLocation,
                routeTo = dto.ToLocation,
                requiredSeats = dto.RequiredSeats,
                busId = dto.BusId,
                tripId = dto.TripId
            };

            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "Validation failed.";

                return RedirectToAction(nameof(SeatBookingBound), routeValues);
            }

            var distinctSeatIds = dto.SeatIds
                .Distinct()
                .ToList();

            var tripResult = await _companySeatManager.CreateTripForSeatBookingBoundAsync(dto);

            if (!tripResult.Success || tripResult.Data == Guid.Empty)
            {
                TempData["Error"] = tripResult.Message ?? "Failed to prepare trip.";

                return RedirectToAction(nameof(SeatBookingBound), routeValues);
            }

            var bookingDto = new CreateCompanySeatBookingDTO
            {
                CompanyId = dto.CompanyId,
                BookingDirection = CompanySeatBookingDirection.Inbound,
                TripId = tripResult.Data,
                SeatIds = distinctSeatIds,
                SeatsCount = distinctSeatIds.Count,
                TripDate = dto.TripDate.Date,
                FromLocation = dto.FromLocation.Trim(),
                ToLocation = dto.ToLocation.Trim(),
                PricePerSeat = dto.PricePerSeat,
                Notes = string.IsNullOrWhiteSpace(dto.Notes)
                    ? null
                    : dto.Notes.Trim(),
                ClientName = dto.ClientName?.Trim(),
                ClientPhone = dto.ClientPhone?.Trim(),
                ClientTripType = dto.ClientTripType
            };

            var result = await _companySeatManager.CreateBookingBoundAsync(bookingDto);

            TempData[result.Success ? "Success" : "Error"] = result.Message ?? (result.Success ? "Booking completed successfully." : "Booking failed.");

            return RedirectToAction(nameof(SeatBookingBound), new
            {
                tripDate = dto.TripDate.ToString("yyyy-MM-dd"),
                direction = dto.Direction,
                routeFrom = dto.FromLocation,
                routeTo = dto.ToLocation,
                requiredSeats = dto.RequiredSeats,
                busId = dto.BusId,
                tripId = tripResult.Data
            });
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> SeatBookingBoundSearch(SeatBookingBoundPageDTO dto)
        {
            ViewBag.Name = GetCurrentFullName();

            dto ??= new SeatBookingBoundPageDTO();

            dto.ActiveTab = "searchTab";

            dto.Search ??= new CompanySeatBookingSearchDTO();
            dto.Search.BookingDirection = CompanySeatBookingDirection.Inbound;

            ViewBag.Name = GetCurrentFullName();
            
            var pageResult = await _companySeatManager.GetSeatBookingBoundPageAsync(dto);
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ",
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));


                return View(nameof(SeatBookingBound), dto);
            }
            var vm = pageResult.Data ?? new SeatBookingBoundPageDTO
            {
                ActiveTab = "searchTab",
                Search = dto.Search
            };

            if (!pageResult.Success)
                ViewBag.ErrorMessage = pageResult.Message ?? "Page failed to load.";

            var searchResult = await _companySeatManager.SearchAsync(vm.Search);

            if (searchResult.Success)
            {
                vm.SearchResults = searchResult.Data
                    ?? new List<CompanyTripSeatSummaryDTO>();
            }
            else
            {
                vm.SearchResults = new List<CompanyTripSeatSummaryDTO>();
                ViewBag.ErrorMessage = searchResult.Message ?? "Search failed.";
            }

            vm.ActiveTab = "searchTab";

            return View(nameof(SeatBookingBound), vm);
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> SeatBookingBoundFindTransfer(SeatBookingBoundPageDTO dto)
        {
            ViewBag.Name = GetCurrentFullName();
            dto.ActiveTab = "transferTab";
            var result = await _companySeatManager.GetSeatBookingBoundTransferPageAsync(dto);
            if (!ModelState.IsValid)
            {
                TempData["Error"] = string.Join(" | ",
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                return View(nameof(SeatBookingBound), dto);
            }

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to load the transfer data.";
                return RedirectToAction(nameof(SeatBookingBound));
            }

            return View(nameof(SeatBookingBound), result.Data);
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeatBookingBoundTransfer(TransferTripSeatsToCompanyDTO dto)
        {
            if (dto.TripId == Guid.Empty)
            {
                TempData["Error"] = "Choose Trip is correct.";
                return RedirectToAction(nameof(SeatBookingBound));
            }

            if (dto.CompanyId == Guid.Empty)
            {
                TempData["Error"] = "Choose the offshore company.";
                return RedirectToAction(nameof(SeatBookingBoundFindTransfer), new
                {
                    tripDate = dto.ExternalTripDate?.ToString("yyyy-MM-dd"),
                    routeFrom = dto.FromLocation,
                    routeTo = dto.ToLocation,
                    tripId = dto.TripId
                });
            }

            dto.BookingSeatIds ??= new List<Guid>();
            dto.CompanySeatBookingIds ??= new List<Guid>();

            if (!dto.BookingSeatIds.Any() && !dto.CompanySeatBookingIds.Any())
            {
                TempData["Error"] = "Choose at least one seat for transportation.";
                return RedirectToAction(nameof(SeatBookingBoundFindTransfer), new
                {
                    tripDate = dto.ExternalTripDate?.ToString("yyyy-MM-dd"),
                    routeFrom = dto.FromLocation,
                    routeTo = dto.ToLocation,
                    tripId = dto.TripId
                });
            }

            var result = await _companySeatManager.TransferTripSeatsToCompanyAsync(dto);

            TempData[result.Success ? "Success" : "Error"] =
                result.Message ?? (result.Success ? "Transfer successful." : "Transfer failed.");

            return RedirectToAction(nameof(SeatBookingBoundFindTransfer), new
            {
                tripDate = dto.ExternalTripDate?.ToString("yyyy-MM-dd"),
                routeFrom = dto.FromLocation,
                routeTo = dto.ToLocation,
                tripId = dto.TripId
            });
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeatBookingBoundDelete(List<Guid> bookingIds, DateTime? dateFrom, DateTime? dateTo, string? location, Guid? companyId, CompanySeatPaymentStatus paymentStatus = CompanySeatPaymentStatus.All)
        {
            if (bookingIds == null || !bookingIds.Any())
            {
                TempData["Error"] = "Choose at least one reservation to delete.";
                return RedirectToAction(nameof(SeatBookingBoundSearch), new RouteValueDictionary
                {
                    ["ActiveTab"] = "searchTab",
                    ["Search.CompanyId"] = companyId,
                    ["Search.DateFrom"] = dateFrom?.ToString("yyyy-MM-dd"),
                    ["Search.DateTo"] = dateTo?.ToString("yyyy-MM-dd"),
                    ["Search.Location"] = location,
                    ["Search.PaymentStatus"] = paymentStatus,
                    ["Search.BookingDirection"] = CompanySeatBookingDirection.Inbound
                });
            }

            bookingIds = bookingIds
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            var result = await _companySeatManager.DeleteSeatBookingsAsync(bookingIds);

            TempData[result.Success ? "Success" : "Error"] =
                result.Message ?? (result.Success ? "Deletion successful.": "Deletion failed.");

            return RedirectToAction(nameof(SeatBookingBoundSearch), new RouteValueDictionary
            {
                ["ActiveTab"] = "searchTab",
                ["Search.CompanyId"] = companyId,
                ["Search.DateFrom"] = dateFrom?.ToString("yyyy-MM-dd"),
                ["Search.DateTo"] = dateTo?.ToString("yyyy-MM-dd"),
                ["Search.Location"] = location,
                ["Search.PaymentStatus"] = paymentStatus,
                ["Search.BookingDirection"] = CompanySeatBookingDirection.Inbound
            });
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeatBookingBoundForceDeleteDetails([FromBody] List<Guid> bookingIds)
        {
            var result = await _companySeatManager.GetForceDeleteConflictDetailsAsync(bookingIds);

            if (!result.Success || result.Data == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message ?? "Failed to load force delete details."
                });
            }

            return Ok(new
            {
                success = true,
                data = result.Data
            });
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeatBookingBoundForceDelete(List<Guid> bookingIds, DateTime? dateFrom, DateTime? dateTo, string? location, Guid? companyId, CompanySeatPaymentStatus paymentStatus = CompanySeatPaymentStatus.All)
        {
            var result = await _companySeatManager.ForceDeleteSeatBookingsAsync(bookingIds);

            TempData[result.Success ? "Success" : "Error"] =
                result.Message ?? (result.Success ? "Force delete completed." : "Force delete failed.");

            return RedirectToAction(nameof(SeatBookingBoundSearch), new RouteValueDictionary
            {
                ["ActiveTab"] = "searchTab",
                ["Search.CompanyId"] = companyId,
                ["Search.DateFrom"] = dateFrom?.ToString("yyyy-MM-dd"),
                ["Search.DateTo"] = dateTo?.ToString("yyyy-MM-dd"),
                ["Search.Location"] = location,
                ["Search.PaymentStatus"] = paymentStatus,
                ["Search.BookingDirection"] = CompanySeatBookingDirection.Inbound
            });
        }
        
        //-----------------------------------------------------------------------////-----------------------------------------------------------------------////-----------------------------------------------------------------------////-----------------------------------------------------------------------//

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> CompanySeatAccounting(Guid? companyId, DateTime? dateFrom, DateTime? dateTo)
        {
            ViewBag.Name = GetCurrentFullName();

            var filter = new CompanySeatAccountingFilterDTO
            {
                CompanyId = companyId,
                DateFrom = dateFrom,
                DateTo = dateTo
            };

            var result = await _companySeatManager.GetCompanySeatAccountingPageAsync(filter);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to load company account.";

                return View(new CompanySeatAccountingPageDTO
                {
                    CompanyId = companyId,
                    DateFrom = dateFrom,
                    DateTo = dateTo
                });
            }

            result.Data.CompanyId = companyId;
            result.Data.DateFrom = dateFrom;
            result.Data.DateTo = dateTo;

            return View(result.Data);
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSeatPrice(Guid companyId, Guid bookingGroupId, decimal pricePerSeat, CompanySeatBookingDirection bookingDirection, DateTime? dateFrom, DateTime? dateTo)
        {
            var result = await _companySeatManager.UpdateSeatPriceAsync(new UpdateCompanySeatPriceDTO
            {
                CompanyId = companyId,
                BookingGroupId = bookingGroupId,
                PricePerSeat = pricePerSeat,
                BookingDirection = bookingDirection
            });

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(CompanySeatAccounting), new { companyId, dateFrom = ToDate(dateFrom), dateTo = ToDate(dateTo) });
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSeatPayment(Guid companyId, Guid? tripId, decimal amount, DateTime paidAt, string? notes, DateTime? dateFrom, DateTime? dateTo)
        {
            var result = await _companySeatManager.AddPaymentAsync(new AddCompanySeatPaymentDTO
            {
                CompanyId = companyId,
                TripId = tripId,
                Amount = amount,
                PaidAt = paidAt == default ? DateTime.Today : paidAt,
                Notes = notes
            });

            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(CompanySeatAccounting), new { companyId, dateFrom = ToDate(dateFrom), dateTo = ToDate(dateTo) });
        }

        [Authorize(Roles = "Admin,Helper")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSeatPayment(Guid companyId, Guid paymentId, DateTime? dateFrom, DateTime? dateTo)
        {
            var result = await _companySeatManager.DeletePaymentAsync(paymentId);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(CompanySeatAccounting), new { companyId, dateFrom = ToDate(dateFrom), dateTo = ToDate(dateTo) });
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> ExportCompanySeatAccountingExcel(Guid companyId, DateTime? dateFrom, DateTime? dateTo)
        {
            var result = await _companySeatManager.GetCompanyAccountAsync(companyId, dateFrom, dateTo);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to export Excel.";
                return RedirectToAction(nameof(CompanySeatAccounting), new
                {
                    companyId,
                    dateFrom = ToDate(dateFrom),
                    dateTo = ToDate(dateTo)
                });
            }

            var account = result.Data;

            using var workbook = new XLWorkbook();

            var orange = XLColor.FromHtml("#FF7A18");
            var darkBlue = XLColor.FromHtml("#172033");
            var lightOrange = XLColor.FromHtml("#FFF3E8");
            var green = XLColor.FromHtml("#16A34A");
            var red = XLColor.FromHtml("#DC2626");
            var white = XLColor.White;

            var summary = workbook.Worksheets.Add("Summary");

            summary.Range("A1:D1").Merge();
            summary.Cell("A1").Value = $"AM Travel - Company Seat Accounting - {account.CompanyName}";
            summary.Cell("A1").Style.Font.Bold = true;
            summary.Cell("A1").Style.Font.FontSize = 18;
            summary.Cell("A1").Style.Font.FontColor = white;
            summary.Cell("A1").Style.Fill.BackgroundColor = orange;
            summary.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            summary.Cell("A3").Value = "Company";
            summary.Cell("B3").Value = account.CompanyName;
            summary.Cell("A4").Value = "Phone";
            summary.Cell("B4").Value = account.CompanyPhone;
            summary.Cell("A5").Value = "Generated At";
            summary.Cell("B5").Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

            if (dateFrom.HasValue || dateTo.HasValue)
            {
                summary.Cell("A6").Value = "Period";
                summary.Cell("B6").Value = $"{dateFrom?.ToString("yyyy-MM-dd") ?? "-"} / {dateTo?.ToString("yyyy-MM-dd") ?? "-"}";
            }

            summary.Cell("A8").Value = "Inbound Total";
            summary.Cell("B8").Value = account.InboundTotal;
            summary.Cell("A9").Value = "Outbound Total";
            summary.Cell("B9").Value = account.OutboundTotal;
            summary.Cell("A10").Value = "Total Paid";
            summary.Cell("B10").Value = account.GrandTotalPaid;
            summary.Cell("A11").Value = "Net Balance";
            summary.Cell("B11").Value = account.NetBalance;

            summary.Range("A3:A11").Style.Font.Bold = true;
            summary.Range("A8:B11").Style.Fill.BackgroundColor = lightOrange;
            summary.Cell("B10").Style.Font.FontColor = green;
            summary.Cell("B11").Style.Font.FontColor = account.NetBalance >= 0 ? green : red;
            summary.Range("A3:B11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            summary.Range("A3:B11").Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            summary.Columns().AdjustToContents();

            var details = workbook.Worksheets.Add("Bookings");

            string[] headers =
            {
        "Date", "Direction", "Type", "Bus / Plate", "Trip Route", "Seat Route",
        "Seats", "Seat Labels", "Client", "Client Phone",
        "Price / Seat", "Total", "Paid", "Remaining", "Notes"
    };

            for (var i = 0; i < headers.Length; i++)
            {
                var cell = details.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = white;
                cell.Style.Fill.BackgroundColor = darkBlue;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            var row = 2;

            foreach (var g in account.TripGroups)
            {
                var seatLabels = string.Join(", ",
                    g.Seats.Select(x =>
                        string.IsNullOrWhiteSpace(x.SeatLabel)
                            ? x.SeatsCount.ToString()
                            : x.SeatLabel));

                var clients = string.Join(" | ",
                    g.Seats.Select(x => x.ClientName)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct());

                var phones = string.Join(" | ",
                    g.Seats.Select(x => x.ClientPhone)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct());

                var notes = string.Join(" | ",
                    g.Seats.Select(x => x.Notes)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct());

                details.Cell(row, 1).Value = g.TripDate.ToString("yyyy-MM-dd");
                details.Cell(row, 2).Value =
                    g.TripDirection == TripDirection.Departure
                        ? "Go"
                        : g.TripDirection == TripDirection.Return
                            ? "Return"
                            : "External";
                details.Cell(row, 3).Value = g.BookingDirection.ToString();
                details.Cell(row, 4).Value = $"{g.BusName}{Environment.NewLine}Plate: {(string.IsNullOrWhiteSpace(g.PlateNumber) ? "-" : g.PlateNumber)}";
                details.Cell(row, 5).Value = $"{g.TripFromLocation} -> {g.TripToLocation}";
                details.Cell(row, 6).Value = $"{g.FromLocation} -> {g.ToLocation}";
                details.Cell(row, 7).Value = g.SeatsCount;
                details.Cell(row, 8).Value = seatLabels;
                details.Cell(row, 9).Value = string.IsNullOrWhiteSpace(clients) ? "-" : clients;
                details.Cell(row, 10).Value = string.IsNullOrWhiteSpace(phones) ? "-" : phones;
                details.Cell(row, 11).Value = g.PricePerSeat;
                details.Cell(row, 12).Value = g.TotalPrice;
                details.Cell(row, 13).Value = g.TotalPaid;
                details.Cell(row, 14).Value = g.TotalRemaining;
                details.Cell(row, 15).Value = notes;

                details.Range(row, 1, row, 15).Style.Alignment.WrapText = true;
                details.Range(row, 1, row, 15).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                details.Range(row, 1, row, 15).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                details.Range(row, 1, row, 15).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                if (g.BookingDirection == CompanySeatBookingDirection.Outbound)
                    details.Range(row, 1, row, 15).Style.Fill.BackgroundColor = lightOrange;

                details.Cell(row, 12).Style.Font.Bold = true;
                details.Cell(row, 13).Style.Font.FontColor = green;
                details.Cell(row, 13).Style.Font.Bold = true;
                details.Cell(row, 14).Style.Font.FontColor = g.TotalRemaining > 0 ? red : green;
                details.Cell(row, 14).Style.Font.Bold = true;

                row++;
            }

            row++;

            details.Cell(row, 10).Value = "Grand Totals";
            details.Cell(row, 12).Value = account.GrandTotalPrice;
            details.Cell(row, 13).Value = account.GrandTotalPaid;
            details.Cell(row, 14).Value = account.GrandTotalRemaining;

            details.Range(row, 10, row, 14).Style.Font.Bold = true;
            details.Range(row, 10, row, 14).Style.Fill.BackgroundColor = orange;
            details.Range(row, 10, row, 14).Style.Font.FontColor = white;

            details.Columns().AdjustToContents();
            details.Rows().AdjustToContents();

            var payments = workbook.Worksheets.Add("Payments");

            string[] pHeaders = { "Date", "Amount", "Trip", "Notes" };

            for (var i = 0; i < pHeaders.Length; i++)
            {
                var cell = payments.Cell(1, i + 1);
                cell.Value = pHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = white;
                cell.Style.Fill.BackgroundColor = darkBlue;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            row = 2;

            foreach (var p in account.Payments)
            {
                payments.Cell(row, 1).Value = p.PaidAt.ToString("yyyy-MM-dd");
                payments.Cell(row, 2).Value = p.Amount;
                payments.Cell(row, 3).Value = p.TripInfo ?? "General Payment";
                payments.Cell(row, 4).Value = p.Notes ?? "";

                payments.Range(row, 1, row, 4).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                payments.Range(row, 1, row, 4).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                row++;
            }

            payments.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CompanySeatAccounting_{SafeFileName(account.CompanyName)}_{DateTime.Now:yyyyMMddHHmm}.xlsx");
        }

        [Authorize(Roles = "Admin,Helper,Viewer")]
        [HttpGet]
        public async Task<IActionResult> ExportCompanySeatAccountingPdf(Guid companyId, DateTime? dateFrom, DateTime? dateTo)
        {
            var result = await _companySeatManager.GetCompanyAccountAsync(companyId, dateFrom, dateTo);

            if (!result.Success || result.Data == null)
            {
                TempData["Error"] = result.Message ?? "Failed to export PDF.";
                return RedirectToAction(nameof(CompanySeatAccounting), new
                {
                    companyId,
                    dateFrom = ToDate(dateFrom),
                    dateTo = ToDate(dateTo)
                });
            }

            QuestPDF.Settings.License = LicenseType.Community;

            var account = result.Data;

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(14);
                    page.DefaultTextStyle(x => x.FontSize(7).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item()
                            .Background("#FF7A18")
                            .Padding(10)
                            .Column(header =>
                            {
                                header.Item().Text($"AM Travel - Company Seat Accounting")
                                    .FontSize(18)
                                    .Bold()
                                    .FontColor(Colors.White);

                                header.Item().Text($"Company: {account.CompanyName} | Phone: {account.CompanyPhone}")
                                    .FontSize(10)
                                    .Bold()
                                    .FontColor(Colors.White);

                                header.Item().Text($"Generated At: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                    .FontSize(9)
                                    .FontColor(Colors.White);

                                if (dateFrom.HasValue || dateTo.HasValue)
                                {
                                    header.Item().Text($"Period: {dateFrom?.ToString("yyyy-MM-dd") ?? "-"} / {dateTo?.ToString("yyyy-MM-dd") ?? "-"}")
                                        .FontSize(9)
                                        .FontColor(Colors.White);
                                }
                            });

                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Background("#172033").Padding(6).Text($"Inbound: {account.InboundTotal:N2}").FontColor(Colors.White).Bold();
                            row.RelativeItem().Background("#DC2626").Padding(6).Text($"Outbound: {account.OutboundTotal:N2}").FontColor(Colors.White).Bold();
                            row.RelativeItem().Background("#16A34A").Padding(6).Text($"Paid: {account.GrandTotalPaid:N2}").FontColor(Colors.White).Bold();
                            row.RelativeItem().Background(account.NetBalance >= 0 ? "#16A34A" : "#DC2626").Padding(6).Text($"Net: {account.NetBalance:N2}").FontColor(Colors.White).Bold();
                        });
                    });

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        col.Item().Text("Bookings Details").FontSize(13).Bold();

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(.8f);
                                c.RelativeColumn(.8f);
                                c.RelativeColumn(.8f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(.6f);
                                c.RelativeColumn(1.2f);
                                c.RelativeColumn(1.1f);
                                c.RelativeColumn(.8f);
                                c.RelativeColumn(.8f);
                                c.RelativeColumn(.8f);
                                c.RelativeColumn(.8f);
                            });

                            string[] headers =
                            {
                        "Date", "Dir", "Type", "Bus", "Trip Route", "Seat Route",
                        "Seats", "Client", "Phone", "Price", "Total", "Paid", "Remain"
                    };

                            table.Header(header =>
                            {
                                foreach (var h in headers)
                                {
                                    header.Cell()
                                        .Background("#172033")
                                        .Border(0.5f)
                                        .BorderColor("#2D3A52")
                                        .Padding(3)
                                        .Text(h)
                                        .FontColor(Colors.White)
                                        .Bold();
                                }
                            });

                            foreach (var g in account.TripGroups)
                            {
                                var bg = g.BookingDirection == CompanySeatBookingDirection.Outbound ? "#FFF3E8" : "#FFFFFF";

                                var clients = string.Join(" | ",
                                    g.Seats.Select(x => x.ClientName)
                                        .Where(x => !string.IsNullOrWhiteSpace(x))
                                        .Distinct());

                                var phones = string.Join(" | ",
                                    g.Seats.Select(x => x.ClientPhone)
                                        .Where(x => !string.IsNullOrWhiteSpace(x))
                                        .Distinct());

                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text(g.TripDate.ToString("yyyy-MM-dd"));
                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text(
                                    g.TripDirection == TripDirection.Departure
                                        ? "Go"
                                        : g.TripDirection == TripDirection.Return
                                            ? "Return"
                                            : "External"
                                );
                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text(g.BookingDirection.ToString());

                                table.Cell().Background(bg).Border(0.5f).Padding(3).Column(c =>
                                {
                                    c.Item().Text(string.IsNullOrWhiteSpace(g.BusName) ? "-" : g.BusName).Bold();
                                    c.Item().Text($"Plate: {(string.IsNullOrWhiteSpace(g.PlateNumber) ? "-" : g.PlateNumber)}");
                                });

                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text($"{g.TripFromLocation} -> {g.TripToLocation}");
                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text($"{g.FromLocation} -> {g.ToLocation}");
                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text(g.SeatsCount.ToString());
                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text(string.IsNullOrWhiteSpace(clients) ? "-" : clients);
                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text(string.IsNullOrWhiteSpace(phones) ? "-" : phones);
                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text(g.PricePerSeat.ToString("N2")).Bold();
                                table.Cell().Background(bg).Border(0.5f).Padding(3).Text(g.TotalPrice.ToString("N2")).Bold();

                                table.Cell().Background(bg).Border(0.5f).Padding(3)
                                    .Text(g.TotalPaid.ToString("N2"))
                                    .FontColor("#16A34A")
                                    .Bold();

                                table.Cell().Background(bg).Border(0.5f).Padding(3)
                                    .Text(g.TotalRemaining.ToString("N2"))
                                    .FontColor(g.TotalRemaining > 0 ? "#DC2626" : "#16A34A")
                                    .Bold();
                            }
                        });

                        col.Item().PaddingTop(10).Text("Payments").FontSize(13).Bold();

                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            string[] pHeaders = { "Date", "Amount", "Trip", "Notes" };

                            table.Header(header =>
                            {
                                foreach (var h in pHeaders)
                                {
                                    header.Cell()
                                        .Background("#FF7A18")
                                        .Border(0.5f)
                                        .Padding(3)
                                        .Text(h)
                                        .FontColor(Colors.White)
                                        .Bold();
                                }
                            });

                            foreach (var p in account.Payments)
                            {
                                table.Cell().Border(0.5f).Padding(3).Text(p.PaidAt.ToString("yyyy-MM-dd"));
                                table.Cell().Border(0.5f).Padding(3).Text(p.Amount.ToString("N2")).FontColor("#16A34A").Bold();
                                table.Cell().Border(0.5f).Padding(3).Text(p.TripInfo ?? "General Payment");
                                table.Cell().Border(0.5f).Padding(3).Text(p.Notes ?? "");
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated by AM Travel - ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });
                });
            }).GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"CompanySeatAccounting_{SafeFileName(account.CompanyName)}_{DateTime.Now:yyyyMMddHHmm}.pdf");
        }

        private static string SafeFileName(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            return string.IsNullOrWhiteSpace(value)
                ? "Company"
                : value.Trim();
        }
        
        private static string? ToDate(DateTime? value) => value?.ToString("yyyy-MM-dd");
    }
}