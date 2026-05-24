using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.CompanyBookSeat
{
    public class UpdateCompanySeatPriceDTO
    {
        [Required]
        public Guid CompanyId { get; set; }

        [Required]
        public Guid BookingGroupId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PricePerSeat { get; set; }

        public CompanySeatBookingDirection BookingDirection { get; set; }
    }
}
