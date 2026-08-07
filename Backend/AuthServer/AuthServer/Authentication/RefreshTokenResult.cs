using AuthServer.Database.Models;

namespace AuthServer.Authentication
{
    public record RefreshTokenResult(string Token, RefreshTokenEntity Entity);
}
