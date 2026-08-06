using AuthServer.Bussiness.Commands;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.Helpers;
using MediatR;

namespace AuthServer.Bussiness.Handlers
{
    internal class CreateNewUserHandler(IUserRepository userRepository) : IRequestHandler<CreateNewUserRequest>
    {
        public async Task Handle(CreateNewUserRequest request, CancellationToken cancellationToken)
        {
            UserEntity newUser = new()
            {
                Login = request.Login,
                PasswordHashed = PasswordHasher.HashPassword(request.Password),
                Role = null // TODO: fix it;
            };
            await userRepository.CreateUserAsync(newUser, cancellationToken).ConfigureAwait(false);
        }
    }
}
