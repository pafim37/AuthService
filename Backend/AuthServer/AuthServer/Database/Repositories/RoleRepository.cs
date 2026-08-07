using AuthServer.DependencyInjection;
using AuthServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Database.Repositories
{
    [Component(typeof(IRoleRepository))]
    public class RoleRepository(AuthContext authContext) : IRoleRepository
    {
        public async Task CreateRoleAsync(RoleEntity role, CancellationToken cancellationToken)
        {
            await authContext.Roles.AddAsync(role, cancellationToken).ConfigureAwait(false);
            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<RoleEntity>> GetAllRolesAsync(CancellationToken cancellationToken)
        {
            return await authContext.Roles
                .Include(role => role.Privileges)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<RoleEntity?> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await authContext.Roles
                .Include(role => role.Privileges)
                .FirstOrDefaultAsync(role => role.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<RoleEntity?> GetRoleByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await authContext.Roles
                .Include(role => role.Privileges)
                .FirstOrDefaultAsync(role => role.Name == name, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task UpdateRoleAsync(RoleEntity role, CancellationToken cancellationToken)
        {
            authContext.Roles.Update(role);
            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveRoleAsync(RoleEntity role, CancellationToken cancellationToken)
        {
            authContext.Roles.Remove(role);
            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
