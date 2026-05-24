namespace BusinessLayer.DTOs.Book
{
    public sealed class BusRouteOptionDTO
    {
        public Guid BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
    }
}
