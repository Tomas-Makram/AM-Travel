namespace BusinessLayer.DTOs.Book
{
    public sealed class RouteBusOptionDTO
    {
        public Guid BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public string OriginalFromLocation { get; set; } = string.Empty;
        public string OriginalToLocation { get; set; } = string.Empty;
    }
}
