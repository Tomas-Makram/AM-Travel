namespace BusinessLayer.DTOs.Company
{
    public sealed class CompanyTripExportRow
    {
        public string Date { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string CompanyPhoneNumber { get; set; } = "";
        public string Type { get; set; } = "";
        public string Bus { get; set; } = "";
        public string Location { get; set; } = "";
        public int TotalSeats { get; set; }
        public int ReservedSeats { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Price { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
    }
}
