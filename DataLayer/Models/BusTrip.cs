using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class BusTrip
    {
        [Key]
        public Guid TripId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BusId { get; set; }

        [ForeignKey(nameof(BusId))]
        public Bus Bus { get; set; } = null!;

        public int LayoutRows { get; set; }

        public int LayoutColumns { get; set; }

        [MaxLength(8000)]
        public string? LayoutJson { get; set; }

        [MaxLength(100)]
        public string BusNameSnapshot { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? PlateNumberSnapshot { get; set; }

        public int SeatsCountSnapshot { get; set; }

        [MaxLength(16000)]
        public string? SeatsSnapshotJson { get; set; }

        [Required]
        public TripDirection Direction { get; set; } = TripDirection.Departure;

        [Required]
        public DateTime TripDate { get; set; }

        [MaxLength(200)]
        public string? FromLocation { get; set; }

        [MaxLength(200)]
        public string? ToLocation { get; set; }

        public bool IsLayoutCustomized { get; set; } = false;

        public bool IsClosed { get; set; } = false;

        public ICollection<BookingTransportationSeat> ReservedSeats { get; set; } = new List<BookingTransportationSeat>();

        public Guid? CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public virtual TransportationCompany? Company { get; set; }

        public Guid? CompanyTripGroupId { get; set; }

        public decimal CompanyTripPrice { get; set; } = 0;
        public decimal CompanyTripPaidAmount { get; set; } = 0;

        [NotMapped]
        public decimal CompanyTripRemainingAmount => Math.Max(0, CompanyTripPrice - CompanyTripPaidAmount);
    }
}