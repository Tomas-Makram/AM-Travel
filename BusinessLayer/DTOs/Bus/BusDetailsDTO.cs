using BusinessLayer.DTOs.Trip;

namespace BusinessLayer.DTOs.Bus
{
    public class BusDetailsDTO
    {
        public Guid BusId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public int SeatsCount { get; set; }
        public int LayoutRows { get; set; }
        public int LayoutColumns { get; set; }
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string? LayoutJson { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
        public List<TripSeatStatusDTO> Seats { get; set; } = new();
    }
}
