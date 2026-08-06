using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RoleController(IRoleRepository roleRepository, IPrivilegeRepository privilegeRepository) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
        {
            IEnumerable<RoleEntity> roles = await roleRepository.GetAllRolesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(roles.Select(ToDto));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetRole(Guid id, CancellationToken cancellationToken)
        {
            RoleEntity? role = await roleRepository.GetRoleByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (role is null)
            {
                return NotFound($"Role with id '{id}' not found.");
            }

            return Ok(ToDto(role));
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole([FromBody] RoleRequestDto roleDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(roleDto.Name))
            {
                return BadRequest("Role name is required.");
            }

            RoleEntity? existingRole = await roleRepository.GetRoleByNameAsync(roleDto.Name, cancellationToken).ConfigureAwait(false);
            if (existingRole is not null)
            {
                return Conflict($"Role with name '{roleDto.Name}' already exists.");
            }

            List<PrivilegeEntity> privileges = await GetPrivileges(roleDto.Privileges, cancellationToken).ConfigureAwait(false);
            if (privileges.Count != roleDto.Privileges.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                return BadRequest("One or more privileges do not exist.");
            }

            RoleEntity role = new()
            {
                Id = Guid.NewGuid(),
                Name = roleDto.Name,
                Privileges = privileges
            };

            await roleRepository.CreateRoleAsync(role, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, ToDto(role));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] RoleRequestDto roleDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(roleDto.Name))
            {
                return BadRequest("Role name is required.");
            }

            RoleEntity? role = await roleRepository.GetRoleByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (role is null)
            {
                return NotFound($"Role with id '{id}' not found.");
            }

            RoleEntity? roleWithSameName = await roleRepository.GetRoleByNameAsync(roleDto.Name, cancellationToken).ConfigureAwait(false);
            if (roleWithSameName is not null && roleWithSameName.Id != id)
            {
                return Conflict($"Role with name '{roleDto.Name}' already exists.");
            }

            List<PrivilegeEntity> privileges = await GetPrivileges(roleDto.Privileges, cancellationToken).ConfigureAwait(false);
            if (privileges.Count != roleDto.Privileges.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                return BadRequest("One or more privileges do not exist.");
            }

            role.Name = roleDto.Name;
            role.Privileges.Clear();
            foreach (PrivilegeEntity privilege in privileges)
            {
                role.Privileges.Add(privilege);
            }

            await roleRepository.UpdateRoleAsync(role, cancellationToken).ConfigureAwait(false);
            return Ok(ToDto(role));
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> PatchRole(Guid id, [FromBody] RolePatchDto roleDto, CancellationToken cancellationToken)
        {
            RoleEntity? role = await roleRepository.GetRoleByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (role is null)
            {
                return NotFound($"Role with id '{id}' not found.");
            }

            if (!string.IsNullOrWhiteSpace(roleDto.Name))
            {
                RoleEntity? roleWithSameName = await roleRepository.GetRoleByNameAsync(roleDto.Name, cancellationToken).ConfigureAwait(false);
                if (roleWithSameName is not null && roleWithSameName.Id != id)
                {
                    return Conflict($"Role with name '{roleDto.Name}' already exists.");
                }

                role.Name = roleDto.Name;
            }

            if (roleDto.Privileges is not null)
            {
                List<PrivilegeEntity> privileges = await GetPrivileges(roleDto.Privileges, cancellationToken).ConfigureAwait(false);
                if (privileges.Count != roleDto.Privileges.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                {
                    return BadRequest("One or more privileges do not exist.");
                }

                role.Privileges.Clear();
                foreach (PrivilegeEntity privilege in privileges)
                {
                    role.Privileges.Add(privilege);
                }
            }

            await roleRepository.UpdateRoleAsync(role, cancellationToken).ConfigureAwait(false);
            return Ok(ToDto(role));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
        {
            RoleEntity? role = await roleRepository.GetRoleByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (role is null)
            {
                return NotFound($"Role with id '{id}' not found.");
            }

            await roleRepository.RemoveRoleAsync(role, cancellationToken).ConfigureAwait(false);
            return NoContent();
        }

        private async Task<List<PrivilegeEntity>> GetPrivileges(IEnumerable<string> privilegeNames, CancellationToken cancellationToken)
        {
            List<PrivilegeEntity> privileges = [];
            foreach (string privilegeName in privilegeNames.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                PrivilegeEntity? privilege = await privilegeRepository.GetPrivilegeByNameAsync(privilegeName, cancellationToken).ConfigureAwait(false);
                if (privilege is not null)
                {
                    privileges.Add(privilege);
                }
            }

            return privileges;
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
                Name = privilege.Name
            };
        }
    }
}
