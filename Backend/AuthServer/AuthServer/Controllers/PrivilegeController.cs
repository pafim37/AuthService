using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/privilege")]
    public class PrivilegeController(IPrivilegeRepository privilegeRepository) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetPrivileges(CancellationToken cancellationToken)
        {
            var privileges = await privilegeRepository.GetAllPrivilegesAsync(cancellationToken);
            return Ok(privileges);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetPrivilege(string name, CancellationToken cancellationToken)
        {
            var privilege = await privilegeRepository.GetPrivilegeByNameAsync(name, cancellationToken);
            if (privilege == null)
            {
                return NotFound($"Privilege with name '{name}' not found.");
            }
            return Ok(privilege);
        }

        [HttpPost("{newPrivilegeName}")]
        public async Task<IActionResult> CreateNewPrivilege(string newPrivilegeName, CancellationToken cancellationToken)
        {
            var privilege = await privilegeRepository.GetPrivilegeByNameAsync(newPrivilegeName, cancellationToken);
            if (privilege != null)
            {
                return Conflict($"Privilege with name '{newPrivilegeName}' already exists.");
            }

            PrivilegeEntity newPrivilege = new() { Name = newPrivilegeName };
            await privilegeRepository.CreatePrivilegeAsync(newPrivilege, cancellationToken);
            return CreatedAtAction(nameof(CreateNewPrivilege), new { name = newPrivilegeName }, newPrivilege);
        }

        [HttpDelete]
        public async Task<IActionResult> DeletePrivilege(string name, CancellationToken cancellationToken)
        {
            var privilegeEntity = await privilegeRepository.GetPrivilegeByNameAsync(name, cancellationToken);
            if (privilegeEntity == null)
            {
                return NotFound($"Privilege with name '{name}' not found.");
            }
            await privilegeRepository.RemovePrivilegeAsync(privilegeEntity, cancellationToken);
            return NoContent();
        }
    }
}