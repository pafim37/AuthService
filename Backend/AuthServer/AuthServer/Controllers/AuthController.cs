using AuthServer.Authentication;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using AuthServer.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IUserRepository userRepository, IRoleRepository roleRepository, JwtTokenService jwtTokenService) : ControllerBase
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
            return Created(string.Empty, jwtTokenService.CreateToken(user));
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

            return Ok(jwtTokenService.CreateToken(user));
        }

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            return Ok("Logout successful. Remove the JWT from the client storage.");
        }

        private static bool ValidateSignUp(NewUserDto signUpDto)
        {
            return !string.IsNullOrWhiteSpace(signUpDto.Login)
                && !string.IsNullOrWhiteSpace(signUpDto.Password)
                && !string.IsNullOrWhiteSpace(signUpDto.Role);
        }
    }
}
