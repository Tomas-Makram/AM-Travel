namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class TransferResultDTO
    {
        public Guid CompanySeatBookingId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public int SeatsTransferred { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string BookingCode { get; set; } = string.Empty;
        public decimal PricePerSeat { get; set; }
        public decimal TotalPrice { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
