using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Auth
{
    public class RefreshTokenRequestDTO
    {
        [Required]
        public Guid SessionId { get; set; }

        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}