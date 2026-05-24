using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.Services
{
    public sealed class JwtSettings
    {
        [Required]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int DurationInMinutes { get; set; } = 60;
    }
}