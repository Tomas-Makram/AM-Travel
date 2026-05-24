using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class BookingAuditDetail
    {
        [Key]
        public Guid DetailId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid AuditId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FieldName { get; set; } = string.Empty;

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        [ForeignKey(nameof(AuditId))]
        public virtual BookingAudit Audit { get; set; } = null!;
    }
}
