using AuthServer.Database.Models;

namespace AuthServer.Database.Repositories
{
    public interface IUserRepository
    {
        public Task<IEnumerable<UserEntity>> GetAllUsersAsync(CancellationToken cancellationToken);
        public Task<UserEntity> GetUserByLoginAsync(string login, CancellationToken cancellationToken);
        public Task CreateUserAsync(UserEntity user, CancellationToken cancellationToken);
        public Task RemoveUserAsync(UserEntity user, CancellationToken cancellationToken);
    }
}
