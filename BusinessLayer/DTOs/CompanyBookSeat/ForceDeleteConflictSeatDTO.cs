namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class ForceDeleteConflictSeatDTO
    {
        public Guid BookingId { get; set; }
        public Guid? TripId { get; set; }
        public Guid? SeatId { get; set; }

        public string? SeatLabel { get; set; }
        public string? CompanyName { get; set; }
        public string? ClientName { get; set; }
        public string? ClientPhone { get; set; }

        public string? BusName { get; set; }
        public DateTime? TripDate { get; set; }
        public string? Route { get; set; }
        public string? Type { get; set; }
    }
}
