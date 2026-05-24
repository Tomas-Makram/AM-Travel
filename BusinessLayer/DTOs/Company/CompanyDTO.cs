namespace BusinessLayer.DTOs.Company
{
    public class CompanyDTO
    {
        public Guid CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsActive { get; set; }
    }
}
