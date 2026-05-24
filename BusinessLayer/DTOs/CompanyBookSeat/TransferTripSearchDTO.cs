namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class TransferTripSearchDTO
    {
        public DateTime? TripDate { get; set; }
        public string? RouteFrom { get; set; }
        public string? RouteTo { get; set; }
        public Guid? SelectedTripId { get; set; }
    }
}
