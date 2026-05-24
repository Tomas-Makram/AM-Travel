using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class BookingAudit
    {
        [Key]
        public Guid AuditId { get; set; }

        [Required]
        public Guid BookingId { get; set; }

        [Required]
        public Guid ChangedByUserId { get; set; }

        [Required]
        public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } = string.Empty; // Create / Update / Delete

        [MaxLength(500)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(BookingId))]
        public virtual BookingData Booking { get; set; } = null!;

        [ForeignKey(nameof(ChangedByUserId))]
        public virtual User ChangedByUser { get; set; } = null!;

        public virtual ICollection<BookingAuditDetail> Details { get; set; } = new List<BookingAuditDetail>();
    }
}
