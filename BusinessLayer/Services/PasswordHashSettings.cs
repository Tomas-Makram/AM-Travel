using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.Services
{
    public sealed class PasswordHashSettings
    {
        [Range(4, 16)]
        public int WorkFactor { get; set; } = 13;
    }
}