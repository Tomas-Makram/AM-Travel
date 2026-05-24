using DataLayer.Models;

namespace BusinessLayer.DTOs.Bus
{
    /// <summary>
    /// يُستخدم لـ Serialize/Deserialize الـ SeatsSnapshotJson في BusTrip.
    /// بيحتفظ بصورة كاملة من الـ Seats وقت إنشاء الـ Trip.
    /// </summary>
    public class TripSeatSnapshotDTO
    {
        public Guid SeatId { get; set; }
        public int SeatNumber { get; set; }
        public string? SeatLabel { get; set; }
        public SeatType SeatType { get; set; }
        public int RowNumber { get; set; }
        public int ColumnNumber { get; set; }
        public bool IsActive { get; set; }
    }
}