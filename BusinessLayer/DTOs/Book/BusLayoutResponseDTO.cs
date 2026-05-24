using BusinessLayer.Functions;

namespace BusinessLayer.DTOs.Book
{
    public sealed class BusLayoutResponseDTO
    {
        public Guid? TripId { get; set; }
        public Guid BusId { get; set; }
        public string BusName { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public string? LayoutJson { get; set; }
        public List<BusLayoutSeatResponseDTO> Seats { get; set; } = new();
    }
}
