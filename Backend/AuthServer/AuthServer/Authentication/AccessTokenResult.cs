namespace AuthServer.Authentication
{
    public record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
}
