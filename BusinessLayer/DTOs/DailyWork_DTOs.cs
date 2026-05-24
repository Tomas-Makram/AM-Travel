using DataLayer.Models;

namespace BusinessLayer.DTOs.Book
{
    public sealed class BookingDailyWorkFilterDTO
    {
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        // transport | hotel
        public string WorkType { get; set; } = "transport";

        // Departure | Return
        public TripDirection? Direction { get; set; } = TripDirection.Departure;

        // CheckIn | CheckOut
        public string HotelDateType { get; set; } = "CheckIn";
    }

    public sealed class BookingDailyWorkPageDTO
    {
        public DateTime SelectedDate { get; set; }
        public string WorkType { get; set; } = "transport";
        public TripDirection? Direction { get; set; } = TripDirection.Departure;
        public string HotelDateType { get; set; } = "CheckIn";

        public List<BookingDailyWorkDayDTO> WeekDays { get; set; } = new();

        public List<BookingDailyTransportTripDTO> TransportTrips { get; set; } = new();

        public List<BookingDailyHotelItemDTO> HotelBookings { get; set; } = new();

        public int TotalBookingsCount { get; set; }
        public int TotalTripsCount { get; set; }
        public int TotalSeatsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalRemaining { get; set; }
    }

    public sealed class BookingDailyWorkDayDTO
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public string ShortDayName { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsToday { get; set; }
    }

    public sealed class BookingDailyCompanySeatDTO
    {
        public string CompanyName { get; set; } = string.Empty;
        public string SeatLabels { get; set; } = string.Empty;
        public int SeatsCount { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public CompanySeatBookingDirection BookingDirection { get; set; }
    }

    public sealed class BookingDailyTransportTripDTO
    {
        public Guid TripId { get; set; }
        public TripDirection Direction { get; set; }
        public DateTime TripDate { get; set; }

        public Guid BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public int BookingsCount { get; set; }
        public int SeatsCount { get; set; }
        public decimal TripTotal { get; set; }
        public decimal TripRemaining { get; set; }

        public int CompanySeatsCount { get; set; }
        public decimal CompanySeatsTotal { get; set; }
        public List<BookingDailyCompanySeatDTO> CompanySeats { get; set; } = new();
        public List<BookingDailyTransportPassengerDTO> Passengers { get; set; } = new();
        public List<BookingDailyCompanyClientDTO> CompanyClients { get; set; } = new();
    }

    public sealed class BookingDailyTransportPassengerDTO
    {
        public Guid BookingId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;

        public string PhoneNumbersText { get; set; } = string.Empty;
        public string RouteText { get; set; } = string.Empty;
        public string SeatLabelsText { get; set; } = string.Empty;

        public int SeatsCount { get; set; }
        public decimal TransportationTotal { get; set; }
        public decimal BookingGrandTotal { get; set; }
        public decimal BookingRemainingAmount { get; set; }
        public decimal BookingPaidAmount { get; set; }

        public bool HasHotel { get; set; }
        public bool HasTransportation { get; set; }
    }

    public sealed class BookingDailyHotelItemDTO
    {
        public Guid BookingId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NightsCount { get; set; }

        public string RoomSummary { get; set; } = string.Empty;
        public int NumberOfRooms { get; set; }

        public int ChildrenCountUntil6Years { get; set; }
        public int ChildrenCountUntil12Years { get; set; }
        public int TotalChildrenCount { get; set; }

        public string PhoneNumbersText { get; set; } = string.Empty;

        public decimal HotelTotal { get; set; }
        public decimal TransportationTotal { get; set; }
        public decimal BookingGrandTotal { get; set; }
        public decimal BookingRemainingAmount { get; set; }
        public decimal BookingPaidAmount { get; set; }

        public bool HasHotel { get; set; }
        public bool HasTransportation { get; set; }
    }


    /// <summary>
    /// حجز شركة Inbound مع بيانات العميل — يظهر في Daily Work جنب الـ passengers
    /// </summary>
    public sealed class BookingDailyCompanyClientDTO
    {
        public Guid BookingId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }
        public string SeatLabel { get; set; } = string.Empty;

        // لو RoundTrip
        public bool HasReturnSeat { get; set; }
        public string? ReturnSeatLabel { get; set; }
        public Guid? ReturnTripId { get; set; }

        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public string RouteText => $"{FromLocation} → {ToLocation}";

        public TransportationTripType ClientTripType { get; set; }

        public decimal PricePerSeat { get; set; }
        public decimal TotalPrice { get; set; }

        public bool IsTransfer { get; set; }
        public string? OriginalBookingCode { get; set; }
    }

    // ─── عدّل BookingDailyTransportTripDTO لإضافة CompanyClients ────────
    // أضف الـ property دي جوا BookingDailyTransportTripDTO:
    //
    //   public List<BookingDailyCompanyClientDTO> CompanyClients { get; set; } = new();
    //
    // بحيث الـ Trip بتعرض الـ passengers (عملاء عندنا) + CompanySeats (شركات بدون عميل)
    // + CompanyClients (شركات حاجزة لعملاء معينين)
}
