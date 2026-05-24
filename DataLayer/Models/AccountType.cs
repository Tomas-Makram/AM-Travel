using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public enum AccountType
    {
        [Display(Name = "Viewer")]
        Viewer = 1,
        
        [Display(Name = "Admin")]
        Admin = 2,

        [Display(Name = "Helper")]
        Helper = 3
    }
}