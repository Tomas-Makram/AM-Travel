using BusinessLayer.Functions;

namespace BusinessLayer.DTOs.Book
{
    public sealed class BookingFormLookupsDTO
    {
        public List<LookupOptionDTO> RoomTypes { get; set; } = new();
        public List<LookupOptionDTO> PayTypes { get; set; } = new();
        public List<BusRouteOptionDTO> BusRoutes { get; set; } = new();
        public List<string> FromLocations { get; set; } = new();
    }
}
