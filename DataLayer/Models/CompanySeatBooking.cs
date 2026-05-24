using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    /// <summary>
    /// Inbound  = الشركة بتحجز منك مقاعد في Bus بتاعك
    ///            TripId + SeatId مطلوبين — المقعد بيتحجز فعلاً
    ///            ممكن يكون معاه بيانات عميل (ClientName, ClientPhone) لو الشركة حاجزة لعميل معين
    ///            لو RoundTrip → ReturnTripId + ReturnSeatId بيتحجزوا برضو
    ///
    /// Outbound = انت بتحجز مقاعد من الشركة في Bus بتاعتها الخارجية
    ///            TripId + SeatId مش موجودين — بس بنسجل العدد والسعر للحساب
    ///            لو جاي من Transfer → TransferredFromBookingId + TransferredFromSeatId موجودين
    /// </summary>
    public class CompanySeatBooking
    {
        [Key]
        public Guid BookingId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public TransportationCompany Company { get; set; } = null!;

        // ── Inbound فقط ───────────────────────────────────────────────
        public Guid? TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public BusTrip? Trip { get; set; }

        public Guid? SeatId { get; set; }

        [ForeignKey(nameof(SeatId))]
        public BusSeat? Seat { get; set; }

        // ── Inbound RoundTrip: رحلة العودة ───────────────────────────
        public Guid? ReturnTripId { get; set; }

        [ForeignKey(nameof(ReturnTripId))]
        public BusTrip? ReturnTrip { get; set; }

        public Guid? ReturnSeatId { get; set; }

        [ForeignKey(nameof(ReturnSeatId))]
        public BusSeat? ReturnSeat { get; set; }

        // ── Seat Snapshot — بيانات المقعد وقت الحجز ───────────────────
        // بتحافظ على بيانات المقعد بغض النظر عن أي تعديل لاحق على Bus/BusSeat
        [MaxLength(50)]
        public string? SeatLabelSnapshot { get; set; }

        public int SeatNumberSnapshot { get; set; }

        public SeatType? SeatTypeSnapshot { get; set; }

        // ── Return Seat Snapshot ───────────────────────────────────────
        [MaxLength(50)]
        public string? ReturnSeatLabelSnapshot { get; set; }

        public int ReturnSeatNumberSnapshot { get; set; }

        public SeatType? ReturnSeatTypeSnapshot { get; set; }

        [MaxLength(16000)]
        public string? TransferredSeatsJson { get; set; }

        // ── Outbound فقط: عدد المقاعد المحجوزة خارجياً ───────────────
        /// <summary>
        /// Inbound  → دايمًا 1 (كل record = مقعد واحد، أو 2 لو RoundTrip)
        /// Outbound → عدد المقاعد المحجوزة من الشركة الخارجية
        /// </summary>
        public int SeatsCount { get; set; } = 1;

        /// <summary>تاريخ الرحلة — للـ Outbound اللي مش عندها TripId</summary>
        public DateTime? TripDate { get; set; }

        /// <summary>تاريخ رحلة العودة — للـ Outbound RoundTrip</summary>
        public DateTime? ReturnTripDate { get; set; }

        [MaxLength(200)]
        public string FromLocation { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ToLocation { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ReturnFromLocation { get; set; } = string.Empty;

        [MaxLength(200)]
        public string ReturnToLocation { get; set; } = string.Empty;
        public decimal PricePerSeat { get; set; } = 0;

        public CompanySeatBookingDirection BookingDirection { get; set; }
            = CompanySeatBookingDirection.Inbound;

        // ── نوع الحجز للعميل (ذهاب / عودة / ذهاب وعودة) ─────────────
        public TransportationTripType ClientTripType { get; set; }
            = TransportationTripType.Departure;

        // ── بيانات العميل (اختيارية — لما الشركة بتحجز لعميل معين) ───
        [MaxLength(200)]
        public string? ClientName { get; set; }

        [MaxLength(20)]
        public string? ClientPhone { get; set; }

        // ── Transfer: لو جاي من نقل عميل من Booking لشركة ────────────
        /// <summary>BookingID من BookingData اللي تم نقل العميل منه</summary>
        public Guid? TransferredFromBookingId { get; set; }

        /// <summary>SeatId اللي كان العميل قاعد فيه في الـ Bus بتاعنا</summary>
        public Guid? TransferredFromSeatId { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // ── Computed ──────────────────────────────────────────────────
        [NotMapped]
        public bool IsRoundTrip => ClientTripType == TransportationTripType.RoundTrip;

        [NotMapped]
        public bool IsTransfer => TransferredFromBookingId.HasValue;

        [NotMapped]
        public decimal TotalPrice => PricePerSeat * SeatsCount;
    }
}