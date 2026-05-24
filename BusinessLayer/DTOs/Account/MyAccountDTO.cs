using DataLayer.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessLayer.DTOs.Account
{
    public class MyAccountDTO
    {
        public Guid UserId { get; set; } = Guid.NewGuid();

        public string UserName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string NationalId { get; set; } = string.Empty;


        // Account Information
        public AccountType AccountType { get; set; } = AccountType.Viewer;
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool Activate { get; set; } = false;
        public bool Login { get; set; } = false;
    }
}