using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/privileges")]
    public class PrivilegeController(IPrivilegeRepository privilegeRepository) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetPrivileges(CancellationToken cancellationToken)
        {
            IEnumerable<PrivilegeEntity> privileges = await privilegeRepository.GetAllPrivilegesAsync(cancellationToken).ConfigureAwait(false);
            return Ok(privileges.Select(ToDto));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetPrivilege(Guid id, CancellationToken cancellationToken)
        {
            PrivilegeEntity? privilege = await privilegeRepository.GetPrivilegeByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (privilege is null)
            {
                return NotFound($"Privilege with id '{id}' not found.");
            }

            return Ok(ToDto(privilege));
        }

        [HttpPost]
        public async Task<IActionResult> CreateNewPrivilege([FromBody] PrivilegeRequestDto privilegeDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(privilegeDto.Name))
            {
                return BadRequest("Privilege name is required.");
            }

            PrivilegeEntity? existingPrivilege = await privilegeRepository.GetPrivilegeByNameAsync(privilegeDto.Name, cancellationToken).ConfigureAwait(false);
            if (existingPrivilege is not null)
            {
                return Conflict($"Privilege with name '{privilegeDto.Name}' already exists.");
            }

            PrivilegeEntity newPrivilege = new()
            {
                Id = Guid.NewGuid(),
                Name = privilegeDto.Name
            };

            await privilegeRepository.CreatePrivilegeAsync(newPrivilege, cancellationToken).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetPrivilege), new { id = newPrivilege.Id }, ToDto(newPrivilege));
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdatePrivilege(Guid id, [FromBody] PrivilegeRequestDto privilegeDto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(privilegeDto.Name))
            {
                return BadRequest("Privilege name is required.");
            }

            PrivilegeEntity? privilege = await privilegeRepository.GetPrivilegeByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (privilege is null)
            {
                return NotFound($"Privilege with id '{id}' not found.");
            }

            PrivilegeEntity? privilegeWithSameName = await privilegeRepository.GetPrivilegeByNameAsync(privilegeDto.Name, cancellationToken).ConfigureAwait(false);
            if (privilegeWithSameName is not null && privilegeWithSameName.Id != id)
            {
                return Conflict($"Privilege with name '{privilegeDto.Name}' already exists.");
            }

            privilege.Name = privilegeDto.Name;
            await privilegeRepository.UpdatePrivilegeAsync(privilege, cancellationToken).ConfigureAwait(false);
            return Ok(ToDto(privilege));
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> PatchPrivilege(Guid id, [FromBody] PrivilegeRequestDto privilegeDto, CancellationToken cancellationToken)
        {
            PrivilegeEntity? privilege = await privilegeRepository.GetPrivilegeByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (privilege is null)
            {
                return NotFound($"Privilege with id '{id}' not found.");
            }

            if (!string.IsNullOrWhiteSpace(privilegeDto.Name))
            {
                PrivilegeEntity? privilegeWithSameName = await privilegeRepository.GetPrivilegeByNameAsync(privilegeDto.Name, cancellationToken).ConfigureAwait(false);
                if (privilegeWithSameName is not null && privilegeWithSameName.Id != id)
                {
                    return Conflict($"Privilege with name '{privilegeDto.Name}' already exists.");
                }

                privilege.Name = privilegeDto.Name;
            }

            await privilegeRepository.UpdatePrivilegeAsync(privilege, cancellationToken).ConfigureAwait(false);
            return Ok(ToDto(privilege));
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeletePrivilege(Guid id, CancellationToken cancellationToken)
        {
            PrivilegeEntity? privilege = await privilegeRepository.GetPrivilegeByIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (privilege is null)
            {
                return NotFound($"Privilege with id '{id}' not found.");
            }

            await privilegeRepository.RemovePrivilegeAsync(privilege, cancellationToken).ConfigureAwait(false);
            return NoContent();
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
