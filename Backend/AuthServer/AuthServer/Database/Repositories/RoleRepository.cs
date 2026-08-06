using AuthServer.Database.Models;

namespace AuthServer.Database.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        public Task CreateRoleAsync(RoleEntity role)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RoleEntity>> GetAllRolesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RoleEntity> GetRoleByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task RemoveRoleAsync(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
