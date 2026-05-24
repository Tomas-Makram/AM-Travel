namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class TransferTripCompanySeatDTO
    {
        public Guid CompanySeatBookingId { get; set; }
        public Guid CompanyId { get; set; }

        public string CompanyName { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;

        public Guid TripId { get; set; }
        public DateTime TripDate { get; set; }

        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;

        public Guid? SeatId { get; set; }
        public string SeatLabel { get; set; } = string.Empty;

        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }

        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;

        public decimal PricePerSeat { get; set; }
    }
}
