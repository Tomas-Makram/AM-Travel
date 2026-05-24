using BusinessLayer.DTOs.Company;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class CompanySeatAccountingPageDTO
    {
        public Guid? CompanyId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public List<SelectListItem> Companies { get; set; } = new();
        public CompanySeatAccountPageDTO? Account { get; set; }
    }
}
