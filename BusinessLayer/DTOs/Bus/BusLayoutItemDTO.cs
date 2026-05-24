namespace BusinessLayer.DTOs.Bus
{
    public class BusLayoutItemDTO
    {
        public int Type { get; set; }
        public int Row { get; set; }
        public int Column { get; set; }
        public string? Label { get; set; }
        public bool IsActive { get; set; } = true;
        public bool HasDoor { get; set; }
    }
}