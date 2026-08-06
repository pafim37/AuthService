using AuthServer.Database.Models;
using AuthServer.DataTransferObjects;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthServer.Authentication
{
    public class JwtTokenService(IConfiguration configuration)
    {
        public AuthTokenDto CreateToken(UserEntity user)
        {
            IConfigurationSection jwtSettings = configuration.GetSection("Jwt");
            string signingKey = jwtSettings["AuthServiceKey"]
                ?? throw new InvalidOperationException("JWT signing key is not configured.");
            string issuer = jwtSettings["Issuer"]
                ?? throw new InvalidOperationException("JWT issuer is not configured.");
            string audience = jwtSettings["Audience"]
                ?? throw new InvalidOperationException("JWT audience is not configured.");

            int expiresInMinutes = int.TryParse(jwtSettings["expiresInMinutes"], out int configuredExpiresInMinutes)
                ? configuredExpiresInMinutes
                : 120;

            DateTime expiresAtUtc = DateTime.UtcNow.AddMinutes(expiresInMinutes);
            List<Claim> claims =
            [
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, user.Login ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Login ?? string.Empty),
                new(ClaimTypes.Role, user.Role?.Name ?? string.Empty)
            ];

            if (user.Role is not null)
            {
                claims.AddRange(user.Role.Privileges
                    .Where(privilege => !string.IsNullOrWhiteSpace(privilege.Name))
                    .Select(privilege => new Claim("privilege", privilege.Name!)));
            }

            SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(signingKey));
            SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return new AuthTokenDto
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAtUtc = expiresAtUtc
            };
        }
    }
}
