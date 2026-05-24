using DataLayer.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BusinessLayer.DTOs.Company
{
    public enum CompanyTripPaymentFilter
    {
        All = 0,
        Paid = 1,
        Unpaid = 2
    }
}