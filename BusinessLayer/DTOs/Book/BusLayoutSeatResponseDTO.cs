namespace BusinessLayer.DTOs.Book
{
    public sealed class BusLayoutSeatResponseDTO
    {
        public Guid SeatId { get; set; }
        public int SeatNumber { get; set; }
        public string Label { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Column { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsReserved { get; set; }
    }
}
