using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    /// <summary>
    /// دفعة مالية مرتبطة بحجز مقاعد شركة.
    /// كل دفعة مرتبطة بشركة، وممكن تكون مرتبطة برحلة بعينها أو عامة للشركة.
    /// </summary>
    public class CompanySeatPayment
    {
        [Key]
        public Guid PaymentId { get; set; } = Guid.NewGuid();

        // ── الشركة ───────────────────────────────────────────────────
        [Required]
        public Guid CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        public TransportationCompany Company { get; set; } = null!;

        // ── الرحلة (اختياري — دفعة ممكن تكون عامة للشركة) ───────────
        public Guid? TripId { get; set; }

        [ForeignKey(nameof(TripId))]
        public BusTrip? Trip { get; set; }

        // ── بيانات الدفعة ─────────────────────────────────────────────
        public decimal Amount { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.Today;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
