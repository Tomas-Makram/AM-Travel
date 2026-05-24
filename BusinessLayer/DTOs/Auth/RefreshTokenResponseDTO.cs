namespace BusinessLayer.DTOs.Auth
{
    public class RefreshTokenResponseDTO
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
        public Guid SessionId { get; set; }
    }
}