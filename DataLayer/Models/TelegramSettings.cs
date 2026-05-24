using System.ComponentModel.DataAnnotations;

namespace DataLayer.Models
{
    public class TelegramSettings
    {
        [Key]
        public int Id { get; set; }

        public string BotToken { get; set; } = string.Empty;
        public int OtpExpireMinutes { get; set; } = 5;

        public string ChatId { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public int TimeErrorOTP { get; set; } = 5;

        public DateTime CreateAt { get; set; }

        public int SendOTPAfterMinuts { get; set; } = 5;
        public DateTime LastSendOTP { get; set; }
    }
}