using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataLayer.Models
{
    public class UserSession
    {
        [Key]
        public Guid SessionId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string RefreshTokenHash { get; set; } = string.Empty;

        [Required]
        public DateTime RefreshTokenExpiresAt { get; set; }

        [Required]
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsActive { get; set; } = true;

        public string? DeviceName { get; set; }
        public string? IpAddress { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;
    }
}