using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public enum TransportationTripType
    {
        [Display(Name = "Departure")]
        Departure = 1,
        [Display(Name = "Return")]
        Return = 2,
        [Display(Name = "Round")]
        RoundTrip = 3
    }
}