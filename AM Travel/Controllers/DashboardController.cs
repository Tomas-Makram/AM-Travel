using BusinessLayer.DTOs.Book;
using BusinessLayer.Functions;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;

namespace AM_Travel.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IAuthenticateManager _authenticate;
        private readonly IBookingManager _bookingManager;

        public DashboardController(IAuthenticateManager authenticate, IBookingManager bookingManager)
        {
            _authenticate = authenticate;
            _bookingManager = bookingManager;
        }

        public async Task<IActionResult> Index(DateTime? date, string? bookingType, bool allWork = false)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return RedirectToAction("Login", "Auth");

            var account = await _authenticate.GetMyAccount(userId);
            if (!account.Success || account.Data == null)
                return RedirectToAction("Login", "Auth");

            var bookingsResult = await _bookingManager.GetAllAsync();

            var bookings = bookingsResult.Data ?? new List<BookingListItemDTO>();
            var selectedDate = date?.Date ?? DateTime.Today;

            bookings = FilterDashboardBookings(bookings, selectedDate, bookingType, allWork);

            ViewBag.Bookings = bookings;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.BookingType = bookingType;
            ViewBag.AllWork = allWork;

            return View(account.Data);
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(DateTime? date, string? bookingType, bool allWork = false)
        {
            var data = await GetDashboardExportData(date, bookingType, allWork);

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Dashboard Work");

            var headers = new[]
            {
                "Code", "Client", "Phones", "Type", "Hotel", "Check In", "Check Out",
                "Rooms", "Room Type", "Payment", "Hotel Total", "Transport Total",
                "Discount", "Grand Total", "Paid", "Remaining", "Notes", "Created By", "Created At"
            };

            for (var i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var row = 2;

            foreach (var b in data)
            {
                ws.Cell(row, 1).Value = b.Code;
                ws.Cell(row, 2).Value = b.ClientName;
                ws.Cell(row, 3).Value = b.PhoneNumbersText;
                ws.Cell(row, 4).Value = GetBookingType(b);
                ws.Cell(row, 5).Value = b.HasHotel ? b.HotelName : "-";
                ws.Cell(row, 6).Value = b.CheckInDate.ToString("yyyy-MM-dd");
                ws.Cell(row, 7).Value = b.CheckOutDate.ToString("yyyy-MM-dd");
                ws.Cell(row, 8).Value = b.HasHotel ? b.NumberOfRooms : 0;
                ws.Cell(row, 9).Value = b.HasHotel ? b.RoomTypeName : "-";
                ws.Cell(row, 10).Value = b.PayTypeName;
                ws.Cell(row, 11).Value = b.HotelTotal;
                ws.Cell(row, 12).Value = b.TransportationTotal;
                ws.Cell(row, 13).Value = b.Discount;
                ws.Cell(row, 14).Value = b.GrandTotal;
                ws.Cell(row, 15).Value = b.PaidAmount;
                ws.Cell(row, 16).Value = b.RemainingAmount;
                ws.Cell(row, 17).Value = b.Notes ?? "";
                ws.Cell(row, 18).Value = b.CreatedBy;
                ws.Cell(row, 19).Value = b.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Row(1).Style.Font.Bold = true;
            ws.Row(1).Style.Fill.BackgroundColor = XLColor.Orange;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            var fileName = $"AMTravel-Work-{DateTime.Now:yyyyMMddHHmm}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(DateTime? date, string? bookingType, bool allWork = false)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var data = await GetDashboardExportData(date, bookingType, allWork);
            var selectedDate = date?.Date ?? DateTime.Today;

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("AM Travel - Daily Work Report").FontSize(18).Bold();
                        col.Item().Text($"Date: {selectedDate:yyyy-MM-dd}");
                        col.Item().Text(allWork ? "Scope: All Work" : "Scope: My Work");
                    });

                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                        });

                        string[] headers =
                        {
                            "Code", "Client", "Phones", "Type", "Hotel", "In", "Out",
                            "Total", "Paid", "Remain", "Notes", "Created By"
                        };

                        table.Header(header =>
                        {
                            foreach (var h in headers)
                            {
                                header.Cell()
                                    .Background(Colors.Orange.Medium)
                                    .Padding(4)
                                    .Text(h)
                                    .Bold();
                            }
                        });

                        foreach (var b in data)
                        {
                            table.Cell().Padding(4).Text(b.Code);
                            table.Cell().Padding(4).Text(b.ClientName);
                            table.Cell().Padding(4).Text(b.PhoneNumbersText);
                            table.Cell().Padding(4).Text(GetBookingType(b));
                            table.Cell().Padding(4).Text(b.HasHotel ? b.HotelName : "-");
                            table.Cell().Padding(4).Text(b.CheckInDate.ToString("MM/dd"));
                            table.Cell().Padding(4).Text(b.CheckOutDate.ToString("MM/dd"));
                            table.Cell().Padding(4).Text(b.GrandTotal.ToString("N2"));
                            table.Cell().Padding(4).Text(b.PaidAmount.ToString("N2"));
                            table.Cell().Padding(4).Text(b.RemainingAmount.ToString("N2"));
                            table.Cell().Padding(4).Text(b.Notes ?? "");
                            table.Cell().Padding(4).Text(b.CreatedBy);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated at ");
                        x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    });
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"AMTravel-Work-{DateTime.Now:yyyyMMddHHmm}.pdf");
        }

        private async Task<List<BookingListItemDTO>> GetDashboardExportData(DateTime? date, string? bookingType, bool allWork)
        {
            var userId = GetCurrentUserId();
            var result = await _bookingManager.GetAllAsync();
            var data = result.Data ?? new List<BookingListItemDTO>();
            var selectedDate = date?.Date ?? DateTime.Today;

            return FilterDashboardBookings(data, selectedDate, bookingType, allWork);
        }

        private static List<BookingListItemDTO> FilterDashboardBookings(List<BookingListItemDTO> data, DateTime selectedDate, string? bookingType, bool allWork)
        {
            if (!allWork)
            {
                data = data.Where(x =>
                        x.CheckInDate.Date == selectedDate ||
                        x.CheckOutDate.Date == selectedDate ||
                        x.CreatedAtUtc.ToLocalTime().Date == selectedDate)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(bookingType))
            {
                data = bookingType switch
                {
                    "hotel" => data.Where(x => x.HasHotel && !x.HasTransportation).ToList(),
                    "transport" => data.Where(x => !x.HasHotel && x.HasTransportation).ToList(),
                    "both" => data.Where(x => x.HasHotel && x.HasTransportation).ToList(),
                    _ => data
                };
            }

            return data;
        }

        private static string GetBookingType(BookingListItemDTO b)
        {
            if (b.HasHotel && b.HasTransportation) return "Hotel + Transport";
            if (b.HasHotel) return "Hotel Only";
            if (b.HasTransportation) return "Transport Only";
            return "N/A";
        }

        private Guid GetCurrentUserId()
        {
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
            return userId;
        }
    }
}