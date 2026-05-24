using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        // Personal Information

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;
        
        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string NationalId { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Account Information
        public string Type { get; set; } = AccountType.Viewer.ToString();

        [Required]
        public string TypeChipher { get; set; } = string.Empty;

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }
        public bool Activate { get; set; } = false;
        public bool Login { get; set; } = false;

        // Account security
        public bool Blocked { get; set; } = false;
        public int FailedLoginAttempts { get; set; } = 0;

        public ICollection<BookingAudit> BookingAudits { get; set; } = new List<BookingAudit>();
    }
}