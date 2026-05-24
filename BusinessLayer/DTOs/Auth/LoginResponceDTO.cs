namespace BusinessLayer.DTOs.Auth
{
    public class LoginResponceDTO
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpireAt { get; set; }
        public Guid UserID { get; set; }

        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpireAt { get; set; }
        public Guid SessionId { get; set; }
    }
}
