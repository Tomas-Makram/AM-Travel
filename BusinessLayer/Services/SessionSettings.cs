using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.Services
{
    public sealed class SessionSettings
    {
        [Range(1, 365)]
        public int RefreshTokenDays { get; set; } = 7;

        [Range(1, 1440)]
        public int IdleTimeoutMinutes { get; set; } = 120;
    }
}