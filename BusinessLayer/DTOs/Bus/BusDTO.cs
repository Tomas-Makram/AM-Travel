namespace BusinessLayer.DTOs.Bus
{
    public class BusDTO
    {
        public Guid BusId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public int SeatsCount { get; set; }
        public int LayoutRows { get; set; }
        public int LayoutColumns { get; set; }
        public string? LayoutJson { get; set; }
        public string? FromLocation { get; set; }
        public string? ToLocation { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }
}
