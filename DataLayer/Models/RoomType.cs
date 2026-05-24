using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public enum RoomType
    {
        [Display(Name ="Single Room")]
        Single = 0,
        [Display(Name ="Double Room")]
        Double = 1,
        [Display(Name = "Triple Room")]
        Triple = 2
    }
}
