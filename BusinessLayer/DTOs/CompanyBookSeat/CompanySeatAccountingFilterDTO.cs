using DataLayer.Models;
using global::BusinessLayer.DTOs.Company;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class CompanySeatAccountingFilterDTO
    {
        public Guid? CompanyId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }
}
