namespace AuthServer.DataTransferObjects
{
    public class AuthTokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}
