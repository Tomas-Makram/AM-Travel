namespace BusinessLayer.DTOs.Book
{
    public class BookingListItemDTO
    {
        public Guid BookingID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public bool HasHotel { get; set; }
        public bool HasTransportation { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfRooms { get; set; }
        public string RoomTypeName { get; set; } = string.Empty;
        public string PayTypeName { get; set; } = string.Empty;
        public decimal HotelTotal { get; set; }
        public decimal TransportationTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal RemainingAmount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public string PhoneNumbersText { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public Guid UserId { get; set; }
    }
}