namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class ForceDeleteConflictDetailsDTO
    {
        public List<Guid> BookingIds { get; set; } = new();

        public DateTime? TripDate { get; set; }
        public string? BusName { get; set; }
        public string? Route { get; set; }

        public string? Message { get; set; }

        public List<ForceDeleteConflictSeatDTO> Seats { get; set; } = new();
    }
}
