namespace AuthServer.DataTransferObjects
{
    public class AuthTokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}
