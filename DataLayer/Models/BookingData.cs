using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class BookingData
    {
        [Key]
        public Guid BookingID { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [Required]
        public string ClientName { get; set; } = string.Empty;

        public virtual ICollection<Telephones> PhoneNumbers { get; set; } = new List<Telephones>();

        public bool HasHotel { get; set; } = true;
        public bool HasTransportation { get; set; } = false;

        public string HotelName { get; set; } = string.Empty;

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        public int NumberOfRooms { get; set; } = 0;
        public RoomType RoomType { get; set; } = RoomType.Single;

        public int ChildrenCountUntil6Years { get; set; } = 0;
        public int ChildrenCountUntil12Years { get; set; } = 0;
        public int TotalChildrenCount { get; set; } = 0;

        public decimal HotelNightPrice { get; set; } = 0;
        public int NightsCount { get; set; } = 0;
        public decimal HotelTotal { get; set; } = 0;

        public int SeatsCount { get; set; } = 0;
        public decimal SeatPrice { get; set; } = 0;
        public decimal TransportationTotal { get; set; } = 0;

        public PayType PayType { get; set; } = PayType.cache;

        public decimal Discount { get; set; } = 0;
        public decimal PaidAmount { get; set; } = 0;
        public decimal GrandTotal { get; set; } = 0;
        public decimal RemainingAmount { get; set; } = 0;

        public string? Notes { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAtUtc { get; set; } = null;

        public Guid? DeletedByUserId { get; set; } = null;

        [ForeignKey(nameof(DeletedByUserId))]
        public virtual User? DeletedByUser { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public virtual ICollection<BookingRoom> Rooms { get; set; } = new List<BookingRoom>();
        public virtual ICollection<BookingPayment> Payments { get; set; } = new List<BookingPayment>();
        public virtual ICollection<BookingTransportationSeat> TransportationSeats { get; set; } = new List<BookingTransportationSeat>();
        public virtual ICollection<BookingAudit> AuditLogs { get; set; } = new List<BookingAudit>();
    }
}