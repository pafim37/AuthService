using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using AuthServer.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers
{
    [ApiController]
    [Authorize(Policy = "FullPrivilege")]
    [Route("api/users")]
    public class UserController(IUserRepository userRepository, IRoleRepository roleRepository) : ControllerBase
    {
        private const string ProtectedAdminLogin = "admin";

        [HttpGet]
        public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
        {
            IEnumerable<UserEntity> users = await userRepository.GetAllUsersAsync(cancellationToken).ConfigureAwait(false);
            return Ok(users.Select(ToDto));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
        {
            UserEntity? user = await userRepository.GetUserByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return NotFound($"User with id '{id}' not found.");
            }

            return Ok(ToDto(user));
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin([FromBody] CredentialsDto newAdminDto, CancellationToken cancellationToken)
        {
            return await CreateNewUser(newAdminDto, "Administrator", cancellationToken).ConfigureAwait(false);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewUser([FromBody] CredentialsDto newUserDto, CancellationToken cancellationToken)
        {
            return await CreateNewUser(newUserDto, "Default", cancellationToken).ConfigureAwait(false);
        }

        private async Task<IActionResult> CreateNewUser(CredentialsDto newUserDto, string roleName, CancellationToken cancellationToken)
        {
            if (!ValidateCredentials(newUserDto))
            {
                return BadRequest("Invalid user data. Please provide valid login and password.");
            }

            UserEntity? existingUser = await userRepository.GetUserByLoginAsync(newUserDto.Login!, cancellationToken).ConfigureAwait(false);
            if (existingUser is not null)
            {
                return Conflict($"User with login '{newUserDto.Login}' already exists.");
            }

            RoleEntity? role = await roleRepository.GetRoleByNameAsync(roleName, cancellationToken).ConfigureAwait(false);
            if (role is null)
            {
                return BadRequest($"Role with name '{roleName}' not found.");
            }

            UserEntity newUser = new()
            {
                Id = Guid.NewGuid(),
                Login = newUserDto.Login,
                PasswordHashed = PasswordHasher.HashPassword(newUserDto.Password!),
                RoleId = role.Id,
                Role = role
            };

            await userRepository.CreateUserAsync(newUser, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, ToDto(newUser));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] CredentialsDto userDto, CancellationToken cancellationToken)
        {
            if (!ValidateCredentials(userDto))
            {
                return BadRequest("Invalid user data. Please provide valid login and password.");
            }

            UserEntity? user = await userRepository.GetUserByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return NotFound($"User with id '{id}' not found.");
            }

            IActionResult? validationResult = await ValidateUserUpdate(id, userDto.Login!, cancellationToken).ConfigureAwait(false);
            if (validationResult is not null)
            {
                return validationResult;
            }

            user.Login = userDto.Login;
            user.PasswordHashed = PasswordHasher.HashPassword(userDto.Password!);

            await userRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
            return Ok(ToDto(user));
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> PatchUser(Guid id, [FromBody] UserPatchDto userDto, CancellationToken cancellationToken)
        {
            UserEntity? user = await userRepository.GetUserByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return NotFound($"User with id '{id}' not found.");
            }

            if (!string.IsNullOrWhiteSpace(userDto.Login))
            {
                UserEntity? userWithSameLogin = await userRepository.GetUserByLoginAsync(userDto.Login, cancellationToken).ConfigureAwait(false);
                if (userWithSameLogin is not null && userWithSameLogin.Id != id)
                {
                    return Conflict($"User with login '{userDto.Login}' already exists.");
                }

                user.Login = userDto.Login;
            }

            if (!string.IsNullOrWhiteSpace(userDto.Password))
            {
                user.PasswordHashed = PasswordHasher.HashPassword(userDto.Password);
            }

            if (!string.IsNullOrWhiteSpace(userDto.Role))
            {
                RoleEntity? role = await roleRepository.GetRoleByNameAsync(userDto.Role, cancellationToken).ConfigureAwait(false);
                if (role is null)
                {
                    return BadRequest($"Role with name '{userDto.Role}' not found.");
                }

                user.RoleId = role.Id;
                user.Role = role;
            }

            await userRepository.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
            return Ok(ToDto(user));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
        {
            UserEntity? user = await userRepository.GetUserByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return NotFound($"User with id '{id}' not found.");
            }

            if (string.Equals(user.Login, ProtectedAdminLogin, StringComparison.OrdinalIgnoreCase))
            {
                return Conflict("Built-in admin user cannot be deleted.");
            }

            await userRepository.RemoveUserAsync(user, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        private async Task<IActionResult?> ValidateUserUpdate(Guid id, string login, CancellationToken cancellationToken)
        {
            UserEntity? userWithSameLogin = await userRepository.GetUserByLoginAsync(login, cancellationToken).ConfigureAwait(false);
            if (userWithSameLogin is not null && userWithSameLogin.Id != id)
            {
                return Conflict($"User with login '{login}' already exists.");
            }

            return null;
        }

        private static bool ValidateCredentials(CredentialsDto newUserDto)
        {
            return !string.IsNullOrWhiteSpace(newUserDto.Login)
                && !string.IsNullOrWhiteSpace(newUserDto.Password);
        }

        private static UserDto ToDto(UserEntity user)
        {
            return new UserDto
            {
                Id = user.Id,
                Login = user.Login,
                Role = user.Role is null ? null : ToDto(user.Role)
            };
        }

        private static RoleDto ToDto(RoleEntity role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Privileges = role.Privileges.Select(ToDto)
            };
        }

        private static PrivilegeDto ToDto(PrivilegeEntity privilege)
        {
            return new PrivilegeDto
            {
                Id = privilege.Id,
                Name = privilege.Name,
                Description = privilege.Description
            };
        }
    }
}
