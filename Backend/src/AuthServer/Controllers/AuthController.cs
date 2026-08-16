using AuthServer.Authentication;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using AuthServer.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
            AuthTokenDto tokens = await CreateTokenPairAsync(user, cancellationToken).ConfigureAwait(false);
            AppendAuthenticationCookies(tokens);
            return Created(string.Empty, tokens);
        }

        [HttpPost("sign-in")]
        [AllowAnonymous]
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

            AuthTokenDto tokens = await CreateTokenPairAsync(user, cancellationToken).ConfigureAwait(false);
            AppendAuthenticationCookies(tokens);
            return Ok(tokens);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto? refreshTokenDto, CancellationToken cancellationToken)
        {
            string? refreshTokenValue = GetRefreshToken(refreshTokenDto);
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
            AuthTokenDto tokens = new()
            {
                AccessToken = accessToken.Token,
                RefreshToken = newRefreshToken.Token,
                ExpiresAtUtc = accessToken.ExpiresAtUtc,
                RefreshTokenExpiresAtUtc = newRefreshToken.Entity.ExpiresAtUtc
            };

            AppendAuthenticationCookies(tokens);
            return Ok(tokens);
        }

        [HttpPost("admin-sign-in")]
        [AllowAnonymous]
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

            if (!string.Equals(user.Role!.Name, "Administrator", StringComparison.OrdinalIgnoreCase))
            {
                
                return StatusCode(StatusCodes.Status403Forbidden, "Access denied. User does not have administrator privileges.");
            }

            AuthTokenDto tokens = await CreateTokenPairAsync(user, cancellationToken).ConfigureAwait(false);
            AppendAuthenticationCookies(tokens);
            return Ok(tokens);
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
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto? refreshTokenDto, CancellationToken cancellationToken)
        {
            string? refreshTokenValue = GetRefreshToken(refreshTokenDto);
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
            return Ok("Logout successful.");
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(changePasswordDto.NewPassword))
            {
                return BadRequest("New password is required.");
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

            user.PasswordHashed = PasswordHasher.HashPassword(changePasswordDto.NewPassword);
            user.SessionVersion++;

            await userRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

            return Ok("Password changed successfully.");
        }

        private async Task<AuthTokenDto> CreateTokenPairAsync(UserEntity user, CancellationToken cancellationToken)
        {
            AccessTokenResult accessToken = jwtTokenService.CreateAccessToken(user);
            RefreshTokenResult refreshToken = await refreshTokenService
                .CreateRefreshTokenAsync(user, cancellationToken)
                .ConfigureAwait(false);

            await refreshTokenService.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new AuthTokenDto
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

        private string? GetRefreshToken(RefreshTokenDto? refreshTokenDto)
        {
            if (!string.IsNullOrWhiteSpace(refreshTokenDto?.RefreshToken))
            {
                return refreshTokenDto.RefreshToken;
            }

            return Request.Cookies.TryGetValue(AuthenticationCookieNames.RefreshToken, out string? refreshToken)
                ? refreshToken
                : null;
        }

        private void AppendAuthenticationCookies(AuthTokenDto tokens)
        {
            Response.Cookies.Append(
                AuthenticationCookieNames.AccessToken,
                tokens.AccessToken,
                CreateCookieOptions(tokens.ExpiresAtUtc));

            Response.Cookies.Append(
                AuthenticationCookieNames.RefreshToken,
                tokens.RefreshToken,
                CreateCookieOptions(tokens.RefreshTokenExpiresAtUtc));
        }

        private void DeleteAuthenticationCookies()
        {
            Response.Cookies.Delete(AuthenticationCookieNames.AccessToken, CreateDeleteCookieOptions());
            Response.Cookies.Delete(AuthenticationCookieNames.RefreshToken, CreateDeleteCookieOptions());
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
    }
}
