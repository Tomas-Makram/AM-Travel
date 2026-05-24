namespace BusinessLayer.DTOs.Book
{
    public sealed class BookingAuditDetailDTO
    {
        public string FieldName { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
