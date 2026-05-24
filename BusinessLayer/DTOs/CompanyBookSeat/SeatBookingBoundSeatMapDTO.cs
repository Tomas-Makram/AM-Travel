using BusinessLayer.DTOs.Company;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class SeatBookingBoundSeatMapDTO
    {
        public Guid? TripId { get; set; }
        public Guid BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int Columns { get; set; }
        public List<SeatBookingBoundSeatDTO> Seats { get; set; } = new();
    }
}
