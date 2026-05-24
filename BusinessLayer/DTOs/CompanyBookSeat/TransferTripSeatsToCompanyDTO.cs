namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class TransferTripSeatsToCompanyDTO
    {
        public Guid TripId { get; set; }

        // مقاعد الأفراد
        public List<Guid> BookingSeatIds { get; set; } = new();

        // مقاعد الشركات المحجوزة عندك
        public List<Guid> CompanySeatBookingIds { get; set; } = new();

        public Guid CompanyId { get; set; }

        public DateTime? ExternalTripDate { get; set; }

        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public decimal PricePerSeat { get; set; }
        public string? Notes { get; set; }
    }
}
