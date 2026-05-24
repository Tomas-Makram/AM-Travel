namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class CompanySeatPaymentDTO
    {
        public Guid PaymentId { get; set; }
        public Guid CompanyId { get; set; }
        public Guid? TripId { get; set; }
        public string? TripInfo { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
