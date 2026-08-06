using AuthServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Database.Repositories
{
    public class UserRepository(AuthContext authContext) : IUserRepository
    {
        public async Task CreateUserAsync(UserEntity user, CancellationToken cancellationToken)
        {
            await authContext.Users.AddAsync(user, cancellationToken).ConfigureAwait(false);
            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<UserEntity>> GetAllUsersAsync(CancellationToken cancellationToken)
        {
            return await authContext.Users
                .Include(user => user.Role)
                .ThenInclude(role => role!.Privileges)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<UserEntity?> GetUserByLoginAsync(string login, CancellationToken cancellationToken)
        {
            return await authContext.Users
                .Include(user => user.Role)
                .ThenInclude(role => role!.Privileges)
                .FirstOrDefaultAsync(u => u.Login == login, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<UserEntity?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await authContext.Users
                .Include(user => user.Role)
                .ThenInclude(role => role!.Privileges)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task UpdateUserAsync(UserEntity user, CancellationToken cancellationToken)
        {
            authContext.Users.Update(user);
            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveUserAsync(UserEntity user, CancellationToken cancellationToken)
        {
            authContext.Users.Remove(user);
            await authContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
