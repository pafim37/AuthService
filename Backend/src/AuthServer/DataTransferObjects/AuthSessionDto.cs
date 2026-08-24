namespace AuthServer.DataTransferObjects
{
    public class AuthSessionDto
    {
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}
