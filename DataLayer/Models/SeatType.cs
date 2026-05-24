using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public enum SeatType
    {
        [Display(Name = "Normal Seat")]
        Normal = 0,

        [Display(Name = "VIP Seat")]
        VIP = 1,

        [Display(Name = "Driver Seat")]
        Driver = 2,

        [Display(Name = "Assistant Seat")]
        Assistant = 3,

        [Display(Name = "Aisle")]
        Aisle = 4,

        [Display(Name = "Door")]
        Door = 5,

        [Display(Name = "Bathroom")]
        Bathroom = 6,

        [Display(Name = "Empty")]
        Empty = 7
    }
}