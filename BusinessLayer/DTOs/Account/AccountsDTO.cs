using DataLayer.Models;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Account
{
    public class AccountsDTO
    {
        [Required]
        public Guid Id { get; set; } = Guid.Empty;
        [Required]
        public string FullName { get; set; } = null!;
        [Required]
        public string Role { get; set; } = AccountType.Viewer.ToString();
        [Required]
        public bool IsBlocked { get; set; } = true;

        public string UserName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public bool Activate { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
