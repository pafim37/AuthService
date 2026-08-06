using AuthServer.Bussiness.Commands;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using MediatR;

namespace AuthServer.Bussiness.Handlers
{
    public class GetUserHandler(IUserRepository userRepository) : IRequestHandler<GetUserRequest, UserEntity>
    {
        public async Task<UserEntity> Handle(GetUserRequest request, CancellationToken cancellationToken)
        {
            return await userRepository.GetUserByLoginAsync(request.Login, cancellationToken).ConfigureAwait(false);
        }
    }
}
