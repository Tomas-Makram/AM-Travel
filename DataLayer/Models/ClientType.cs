using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public enum ClientType
    {

        [Display(Name = "One Star")]
        oneStar = 1,
        [Display(Name = "Two Star")]
        twoStar = 2,
        [Display(Name = "Three Star")]
        threeStar = 3,
        [Display(Name = "Four Star")]
        fourStar = 4,
        [Display(Name = "Five Star")]
        fiveStar = 5,
    }
}
