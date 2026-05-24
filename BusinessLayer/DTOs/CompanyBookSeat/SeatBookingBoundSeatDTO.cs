using DataLayer.Models;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class SeatBookingBoundSeatDTO
    {
        public Guid SeatId { get; set; }
        public int SeatNumber { get; set; }
        public string? SeatLabel { get; set; }
        public SeatType SeatType { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public bool IsActive { get; set; }
        public bool IsClientReserved { get; set; }
        public bool IsCompanyReserved { get; set; }
        public bool IsSelectable { get; set; }
    }
}
