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
        RefreshTokenService refreshTokenService) : ControllerBase
    {
        [HttpPost("sign-up")]
        [AllowAnonymous]
        public async Task<IActionResult> SignUp([FromBody] NewUserDto signUpDto, CancellationToken cancellationToken)
        {
            if (!ValidateSignUp(signUpDto))
            {
                return BadRequest("Invalid user data. Please provide valid login, password, and role.");
            }

            UserEntity? existingUser = await userRepository.GetUserByLoginAsync(signUpDto.Login!, cancellationToken).ConfigureAwait(false);
            if (existingUser is not null)
            {
                return Conflict($"User with login '{signUpDto.Login}' already exists.");
            }

            RoleEntity? role = await roleRepository.GetRoleByNameAsync(signUpDto.Role!, cancellationToken).ConfigureAwait(false);
            if (role is null)
            {
                return BadRequest($"Role with name '{signUpDto.Role}' not found.");
            }

            UserEntity user = new()
            {
                Id = Guid.NewGuid(),
                Login = signUpDto.Login,
                PasswordHashed = PasswordHasher.HashPassword(signUpDto.Password!),
                RoleId = role.Id,
                Role = role
            };

            await userRepository.CreateUserAsync(user, cancellationToken).ConfigureAwait(false);
            AuthTokenDto tokens = await CreateTokenPairAsync(user, cancellationToken).ConfigureAwait(false);
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

            return Ok(await CreateTokenPairAsync(user, cancellationToken).ConfigureAwait(false));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }

            RefreshTokenEntity? storedRefreshToken = await refreshTokenService
                .GetActiveRefreshTokenAsync(refreshTokenDto.RefreshToken, cancellationToken)
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
            return Ok(new AuthTokenDto
            {
                AccessToken = accessToken.Token,
                RefreshToken = newRefreshToken.Token,
                ExpiresAtUtc = accessToken.ExpiresAtUtc,
                RefreshTokenExpiresAtUtc = newRefreshToken.Entity.ExpiresAtUtc
            });
        }

        [HttpPost("admin-sign-in")]
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

            if (user.Role!.Name != "administrator")
            {
                return Unauthorized("Only administrators can sign in with this endpoint.");
            }

            return Ok(await CreateTokenPairAsync(user, cancellationToken).ConfigureAwait(false));
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }

            RefreshTokenEntity? storedRefreshToken = await refreshTokenService
                .GetActiveRefreshTokenAsync(refreshTokenDto.RefreshToken, cancellationToken)
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

            return Ok("Logout successful. Remove the access token from the client storage.");
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

        private static bool ValidateSignUp(NewUserDto signUpDto)
        {
            return !string.IsNullOrWhiteSpace(signUpDto.Login)
                && !string.IsNullOrWhiteSpace(signUpDto.Password)
                && !string.IsNullOrWhiteSpace(signUpDto.Role);
        }
    }
}
