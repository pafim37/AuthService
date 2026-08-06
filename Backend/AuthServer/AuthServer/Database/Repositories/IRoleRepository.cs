using AuthServer.Database.Models;

namespace AuthServer.Database.Repositories
{
    public interface IRoleRepository
    {
        public Task<IEnumerable<RoleEntity>> GetAllRolesAsync();
        public Task<RoleEntity> GetRoleByIdAsync(Guid id);
        public Task CreateRoleAsync(RoleEntity role);
        public Task RemoveRoleAsync(Guid id);
    }
}
