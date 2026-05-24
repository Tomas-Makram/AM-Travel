using DataLayer.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class BookingTransportationSeat
{
    [Key]
    public Guid BookingSeatId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid BookingId { get; set; }

    [ForeignKey(nameof(BookingId))]
    public BookingData Booking { get; set; } = null!;

    [Required]
    public Guid TripId { get; set; }

    [ForeignKey(nameof(TripId))]
    public BusTrip Trip { get; set; } = null!;

    // ده بقى Snapshot SeatId فقط، مش FK على BusSeats
    [Required]
    public Guid SeatId { get; set; }

    public TripDirection Direction { get; set; }

    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;

    public decimal SeatPrice { get; set; }

    public DateTime ReservedAtUtc { get; set; } = DateTime.UtcNow;
}