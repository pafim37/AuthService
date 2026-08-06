using AuthServer.Database.Models;

namespace AuthServer.Database.Repositories
{
    public interface IRoleRepository
    {
        public Task<IEnumerable<RoleEntity>> GetAllRolesAsync(CancellationToken cancellationToken);
        public Task<RoleEntity?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken);
        public Task<RoleEntity?> GetRoleByNameAsync(string name, CancellationToken cancellationToken);
        public Task CreateRoleAsync(RoleEntity role, CancellationToken cancellationToken);
        public Task UpdateRoleAsync(RoleEntity role, CancellationToken cancellationToken);
        public Task RemoveRoleAsync(RoleEntity role, CancellationToken cancellationToken);
    }
}
