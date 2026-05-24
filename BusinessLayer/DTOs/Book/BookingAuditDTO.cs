namespace BusinessLayer.DTOs.Book
{
    public sealed class BookingAuditDTO
    {
        public Guid AuditId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAtUtc { get; set; }
        public string? Notes { get; set; }
        public List<BookingAuditDetailDTO> Details { get; set; } = new();
    }
}
