using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public enum PayType
    {

        [Display(Name = "Cache")]
        cache = 0,
        [Display(Name = "Insta Pay")]
        payment = 1,
        [Display(Name = "Vodafone Cash")]
        vodavonecash = 2,
        [Display(Name = "Etisalat Cash")]
        etisalatcash = 3,
        [Display(Name = "Orange Cash")]
        orangecash = 4,
        [Display(Name = "Visa")]
        visa = 5,
    }
}
