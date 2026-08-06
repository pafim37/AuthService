using AuthServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Database.Repositories
{
    public class PrivilegeRepository(AuthContext authContext) : IPrivilegeRepository
    {

        public async Task CreatePrivilegeAsync(PrivilegeEntity privilege, CancellationToken cancellationToken)
        {
            await authContext.Privileges.AddAsync(privilege, cancellationToken).ConfigureAwait(false);
            await authContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<PrivilegeEntity>> GetAllPrivilegesAsync(CancellationToken cancellationToken)
        {
            return await authContext.Privileges.ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<PrivilegeEntity> GetPrivilegeByNameAsync(string name, CancellationToken cancellationToken)
        {
            return await authContext.Privileges.FirstOrDefaultAsync(p => p.Name == name, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemovePrivilegeAsync(PrivilegeEntity privilege, CancellationToken cancellationToken)
        {
            authContext.Privileges.Remove(privilege);
            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
