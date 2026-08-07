using AuthServer.DependencyInjection;
using AuthServer.Database;
using AuthServer.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace AuthServer.Authentication
{
    [Component(typeof(RefreshTokenService))]
    public class RefreshTokenService(AuthContext authContext, IConfiguration configuration)
    {
        public async Task<RefreshTokenResult> CreateRefreshTokenAsync(UserEntity user, CancellationToken cancellationToken)
        {
            string token = GenerateRefreshToken();
            RefreshTokenEntity refreshToken = new()
            {
                Id = Guid.NewGuid(),
                TokenHash = HashToken(token),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeInDays()),
                UserId = user.Id,
                User = user
            };

            await authContext.RefreshTokens.AddAsync(refreshToken, cancellationToken).ConfigureAwait(false);
            return new RefreshTokenResult(token, refreshToken);
        }

        public async Task<RefreshTokenEntity?> GetActiveRefreshTokenAsync(string token, CancellationToken cancellationToken)
        {
            string tokenHash = HashToken(token);
            RefreshTokenEntity? refreshToken = await authContext.RefreshTokens
                .Include(storedToken => storedToken.User)
                .ThenInclude(user => user!.Role)
                .ThenInclude(role => role!.Privileges)
                .FirstOrDefaultAsync(storedToken => storedToken.TokenHash == tokenHash, cancellationToken)
                .ConfigureAwait(false);

            return refreshToken?.IsActive == true ? refreshToken : null;
        }

        public async Task RevokeRefreshTokenAsync(RefreshTokenEntity refreshToken, Guid? replacedByTokenId, CancellationToken cancellationToken)
        {
            refreshToken.RevokedAtUtc = DateTime.UtcNow;
            refreshToken.ReplacedByTokenId = replacedByTokenId;
            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public static string HashToken(string token)
        {
            byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
            byte[] hashBytes = SHA256.HashData(tokenBytes);
            return Convert.ToBase64String(hashBytes);
        }

        private static string GenerateRefreshToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }

        private int GetRefreshTokenLifetimeInDays()
        {
            string? configuredValue = configuration.GetSection("Jwt")["refreshTokenExpiresInDays"];
            return int.TryParse(configuredValue, out int days) ? days : 7;
        }
    }
}
