using AuthServer.Authentication;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using AuthServer.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Security.Cryptography;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        JwtTokenService jwtTokenService,
        RefreshTokenService refreshTokenService,
        IWebHostEnvironment webHostEnvironment) : ControllerBase
    {
        [HttpPost("sign-up")]
        [AllowAnonymous]
        [EnableRateLimiting("Authentication")]
        public async Task<IActionResult> SignUp([FromBody] CredentialsDto credetialsDto, CancellationToken cancellationToken)
        {
            if (!ValidateCredentials(credetialsDto))
            {
                return BadRequest("Invalid user data. Please provide valid login and password.");
            }

            UserEntity? existingUser = await userRepository.GetUserByLoginAsync(credetialsDto.Login!, cancellationToken).ConfigureAwait(false);
            if (existingUser is not null)
            {
                return Conflict($"User with login '{credetialsDto.Login}' already exists.");
            }

            RoleEntity? role = await roleRepository.GetRoleByNameAsync("Default", cancellationToken).ConfigureAwait(false);
            if (role is null)
            {
                return BadRequest($"Role with name 'Default' not found.");
            }

            UserEntity user = new()
            {
                Id = Guid.NewGuid(),
                Login = credetialsDto.Login,
                PasswordHashed = PasswordHasher.HashPassword(credetialsDto.Password!),
                RoleId = role.Id,
                Role = role
            };

            await userRepository.CreateUserAsync(user, cancellationToken).ConfigureAwait(false);
            AuthTokenPair tokens = await CreateTokenPairAsync(user, cancellationToken).ConfigureAwait(false);
            AppendAuthenticationCookies(tokens);
            return Created(string.Empty, ToSessionDto(tokens));
        }

        [HttpPost("sign-in")]
        [AllowAnonymous]
        [EnableRateLimiting("Authentication")]
        public async Task<IActionResult> SignIn([FromBody] SignInDto signInDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(signInDto.Login) || string.IsNullOrWhiteSpace(signInDto.Password))
            {
                return BadRequest("Login and password are required.");
            }

            UserEntity? user = await userRepository.GetUserByLoginAsync(signInDto.Login, cancellationToken).ConfigureAwait(false);
            if (user is null || string.IsNullOrWhiteSpace(user.PasswordHashed))
            {
                return Unauthorized("Invalid login or password.");
            }

            bool isPasswordValid = PasswordHasher.VerifyPassword(signInDto.Password, user.PasswordHashed);
            if (!isPasswordValid)
            {
                return Unauthorized("Invalid login or password.");
            }

            AuthTokenPair tokens = await CreateTokenPairAsync(user, cancellationToken).ConfigureAwait(false);
            AppendAuthenticationCookies(tokens);
            return Ok(ToSessionDto(tokens));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("Authentication")]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            string? refreshTokenValue = GetRefreshToken();
            if (string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                return BadRequest("Refresh token is required.");
            }

            RefreshTokenEntity? storedRefreshToken = await refreshTokenService
                .GetActiveRefreshTokenAsync(refreshTokenValue, cancellationToken)
                .ConfigureAwait(false);

            if (storedRefreshToken?.User is null)
            {
                return Unauthorized("Invalid refresh token.");
            }

            UserEntity user = storedRefreshToken.User;
            RefreshTokenResult newRefreshToken = await refreshTokenService
                .CreateRefreshTokenAsync(user, cancellationToken)
                .ConfigureAwait(false);

            await refreshTokenService
                .RevokeRefreshTokenAsync(storedRefreshToken, newRefreshToken.Entity.Id, cancellationToken)
                .ConfigureAwait(false);

            AccessTokenResult accessToken = jwtTokenService.CreateAccessToken(user);
            AuthTokenPair tokens = new()
            {
                AccessToken = accessToken.Token,
                RefreshToken = newRefreshToken.Token,
                ExpiresAtUtc = accessToken.ExpiresAtUtc,
                RefreshTokenExpiresAtUtc = newRefreshToken.Entity.ExpiresAtUtc
            };

            AppendAuthenticationCookies(tokens);
            return Ok(ToSessionDto(tokens));
        }

        [HttpPost("admin-sign-in")]
        [AllowAnonymous]
        [EnableRateLimiting("Authentication")]
        public async Task<IActionResult> AdminSignIn([FromBody] SignInDto signInDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(signInDto.Login) || string.IsNullOrWhiteSpace(signInDto.Password))
            {
                return BadRequest("Login and password are required.");
            }

            UserEntity? user = await userRepository.GetUserByLoginAsync(signInDto.Login, cancellationToken).ConfigureAwait(false);
            if (user is null || string.IsNullOrWhiteSpace(user.PasswordHashed))
            {
                return Unauthorized("Invalid login or password.");
            }

            bool isPasswordValid = PasswordHasher.VerifyPassword(signInDto.Password, user.PasswordHashed);
            if (!isPasswordValid)
            {
                return Unauthorized("Invalid login or password.");
            }

            if (!string.Equals(user.Role?.Name, "Administrator", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "Access denied. User does not have administrator privileges.");
            }

            AuthTokenPair tokens = await CreateTokenPairAsync(user, cancellationToken).ConfigureAwait(false);
            AppendAuthenticationCookies(tokens);
            return Ok(ToSessionDto(tokens));
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            string? login = User.FindFirstValue(ClaimTypes.Name);
            if (string.IsNullOrWhiteSpace(login))
            {
                return Unauthorized("Invalid user.");
            }

            return Ok(new { Login = login });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            string? refreshTokenValue = GetRefreshToken();
            if (string.IsNullOrWhiteSpace(refreshTokenValue))
            {
                DeleteAuthenticationCookies();
                return BadRequest("Refresh token is required.");
            }

            RefreshTokenEntity? storedRefreshToken = await refreshTokenService
                .GetActiveRefreshTokenAsync(refreshTokenValue, cancellationToken)
                .ConfigureAwait(false);

            if (storedRefreshToken is not null)
            {
                await refreshTokenService.RevokeRefreshTokenAsync(storedRefreshToken, null, cancellationToken).ConfigureAwait(false);
            }

            string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdValue, out Guid userId))
            {
                UserEntity? user = await userRepository.GetUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
                if (user is not null)
                {
                    user.SessionVersion++;
                    await userRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
                }
            }

            DeleteAuthenticationCookies();
            return Ok();
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(changePasswordDto.CurrentPassword)
                || string.IsNullOrWhiteSpace(changePasswordDto.NewPassword))
            {
                return BadRequest("Current password and new password are required.");
            }

            string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdValue, out Guid userId))
            {
                return Unauthorized("Invalid user.");
            }

            UserEntity? user = await userRepository.GetUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (user is null || string.IsNullOrWhiteSpace(user.PasswordHashed))
            {
                return Unauthorized("Invalid user.");
            }

            if (!PasswordHasher.VerifyPassword(changePasswordDto.CurrentPassword, user.PasswordHashed))
            {
                return Unauthorized("Current password is invalid.");
            }

            user.PasswordHashed = PasswordHasher.HashPassword(changePasswordDto.NewPassword);
            user.SessionVersion++;

            await userRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

            return Ok();
        }

        private async Task<AuthTokenPair> CreateTokenPairAsync(UserEntity user, CancellationToken cancellationToken)
        {
            AccessTokenResult accessToken = jwtTokenService.CreateAccessToken(user);
            RefreshTokenResult refreshToken = await refreshTokenService
                .CreateRefreshTokenAsync(user, cancellationToken)
                .ConfigureAwait(false);

            await refreshTokenService.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new AuthTokenPair
            {
                AccessToken = accessToken.Token,
                RefreshToken = refreshToken.Token,
                ExpiresAtUtc = accessToken.ExpiresAtUtc,
                RefreshTokenExpiresAtUtc = refreshToken.Entity.ExpiresAtUtc
            };
        }

        private static bool ValidateCredentials(CredentialsDto credetialsDto)
        {
            return !string.IsNullOrWhiteSpace(credetialsDto.Login)
                && !string.IsNullOrWhiteSpace(credetialsDto.Password);
        }

        private string? GetRefreshToken()
        {
            return Request.Cookies.TryGetValue(AuthenticationCookieNames.RefreshToken, out string? refreshToken)
                ? refreshToken
                : null;
        }

        private void AppendAuthenticationCookies(AuthTokenPair tokens)
        {
            Response.Cookies.Append(
                AuthenticationCookieNames.AccessToken,
                tokens.AccessToken,
                CreateCookieOptions(tokens.ExpiresAtUtc));

            Response.Cookies.Append(
                AuthenticationCookieNames.RefreshToken,
                tokens.RefreshToken,
                CreateCookieOptions(tokens.RefreshTokenExpiresAtUtc));

            Response.Cookies.Append(
                AuthenticationCookieNames.CsrfToken,
                GenerateCsrfToken(),
                CreateCsrfCookieOptions(tokens.RefreshTokenExpiresAtUtc));
        }

        private void DeleteAuthenticationCookies()
        {
            Response.Cookies.Delete(AuthenticationCookieNames.AccessToken, CreateDeleteCookieOptions());
            Response.Cookies.Delete(AuthenticationCookieNames.RefreshToken, CreateDeleteCookieOptions());
            Response.Cookies.Delete(AuthenticationCookieNames.CsrfToken, CreateDeleteCookieOptions());
        }

        private static AuthSessionDto ToSessionDto(AuthTokenPair tokens)
        {
            return new AuthSessionDto
            {
                ExpiresAtUtc = tokens.ExpiresAtUtc,
                RefreshTokenExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc
            };
        }

        private CookieOptions CreateCookieOptions(DateTime expiresAtUtc)
        {
            return new()
            {
                HttpOnly = true,
                Secure = ShouldUseSecureCookies(),
                SameSite = SameSiteMode.Strict,
                Expires = new DateTimeOffset(expiresAtUtc),
                Path = "/"
            };
        }

        private CookieOptions CreateDeleteCookieOptions()
        {
            return new()
            {
                Secure = ShouldUseSecureCookies(),
                SameSite = SameSiteMode.Strict,
                Path = "/"
            };
        }

        private bool ShouldUseSecureCookies()
        {
            return !webHostEnvironment.IsDevelopment() || Request.IsHttps;
        }

        private CookieOptions CreateCsrfCookieOptions(DateTime expiresAtUtc)
        {
            return new()
            {
                HttpOnly = false,
                Secure = ShouldUseSecureCookies(),
                SameSite = SameSiteMode.Strict,
                Expires = new DateTimeOffset(expiresAtUtc),
                Path = "/"
            };
        }

        private static string GenerateCsrfToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        }

        private sealed class AuthTokenPair
        {
            public string AccessToken { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public DateTime ExpiresAtUtc { get; set; }
            public DateTime RefreshTokenExpiresAtUtc { get; set; }
        }
    }
}
