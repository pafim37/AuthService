using AuthServer.Database.Models;

namespace AuthServer.Database.Repositories
{
    public interface IUserRepository
    {
        public Task<IEnumerable<UserEntity>> GetAllUsersAsync(CancellationToken cancellationToken);
        public Task<UserEntity?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken);
        public Task<UserEntity?> GetUserByLoginAsync(string login, CancellationToken cancellationToken);
        public Task CreateUserAsync(UserEntity user, CancellationToken cancellationToken);
        public Task UpdateUserAsync(UserEntity user, CancellationToken cancellationToken);
        public Task RemoveUserAsync(UserEntity user, CancellationToken cancellationToken);
        public Task IncrementSessionVersionForRoleAsync(Guid roleId, CancellationToken cancellationToken);
        public Task IncrementSessionVersionForRolesWithPrivilegeAsync(Guid privilegeId, CancellationToken cancellationToken);
    }
}
