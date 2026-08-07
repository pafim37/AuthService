using AuthServer.Database.Models;

namespace AuthServer.Database.Repositories
{
    public interface IPrivilegeRepository
    {
        public Task<IEnumerable<PrivilegeEntity>> GetAllPrivilegesAsync(CancellationToken cancellationToken);
        public Task<PrivilegeEntity?> GetPrivilegeByIdAsync(Guid id, CancellationToken cancellationToken);
        public Task<PrivilegeEntity?> GetPrivilegeByNameAsync(string name, CancellationToken cancellationToken);
        public Task CreatePrivilegeAsync(PrivilegeEntity privilege, CancellationToken cancellationToken);
        public Task UpdatePrivilegeAsync(PrivilegeEntity privilege, CancellationToken cancellationToken);
        public Task RemovePrivilegeAsync(PrivilegeEntity privilege, CancellationToken cancellationToken);
    }
}
