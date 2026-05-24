namespace BusinessLayer.DTOs.Company
{
    // ═══════════════════════════════════════════════════════════════════
    // DTOs: تقرير كل الرحلات اللي طلعت فيها عملاء مع شركات (Outbound Transfers)
    // ═══════════════════════════════════════════════════════════════════

    public class OutboundTransferSummaryDTO
    {
        public List<OutboundTransferByCompanyDTO> ByCompany { get; set; } = new();

        public decimal GrandTotalOwed { get; set; }
        public decimal GrandTotalPaid { get; set; }
        public decimal GrandNetOwed { get; set; }
    }

    public class OutboundTransferByCompanyDTO
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;

        public List<OutboundTransferItemDTO> Transfers { get; set; } = new();

        public decimal TotalOwed { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal NetOwed { get; set; }
    }

    public class OutboundTransferItemDTO
    {
        public Guid CompanySeatBookingId { get; set; }
        public DateTime? TripDate { get; set; }
        public string FromLocation { get; set; } = string.Empty;
        public string ToLocation { get; set; } = string.Empty;
        public string RouteText => $"{FromLocation} → {ToLocation}";
        public int SeatsCount { get; set; }
        public decimal PricePerSeat { get; set; }
        public decimal TotalPrice { get; set; }
        public string? ClientName { get; set; }
        public string? OriginalBookingCode { get; set; }
        public string? Notes { get; set; }
    }
}
