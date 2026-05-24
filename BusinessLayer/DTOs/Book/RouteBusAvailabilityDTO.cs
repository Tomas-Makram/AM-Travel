namespace BusinessLayer.DTOs.Book
{
    public sealed class RouteBusAvailabilityDTO
    {
        public Guid BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string PlateNumber { get; set; } = string.Empty;
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public Guid? TripId { get; set; }
        public bool CreateNewTrip { get; set; }
        public int SeatsCount { get; set; }
        public int ReservedCount { get; set; }
        public int AvailableCount { get; set; }
    }
}
