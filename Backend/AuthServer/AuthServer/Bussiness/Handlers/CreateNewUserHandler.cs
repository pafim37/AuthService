using AuthServer.Bussiness.Commands;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.Helpers;
using MediatR;

namespace AuthServer.Bussiness.Handlers
{
    internal class CreateNewUserHandler(IUserRepository userRepository, IRoleRepository roleRepository) : IRequestHandler<CreateNewUserRequest>
    {
        public async Task Handle(CreateNewUserRequest request, CancellationToken cancellationToken)
        {
            RoleEntity role = await roleRepository.GetRoleByNameAsync(request.Role, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Role with name '{request.Role}' not found.");

            UserEntity newUser = new()
            {
                Id = Guid.NewGuid(),
                Login = request.Login,
                PasswordHashed = PasswordHasher.HashPassword(request.Password),
                RoleId = role.Id,
                Role = role
            };
            await userRepository.CreateUserAsync(newUser, cancellationToken).ConfigureAwait(false);
        }
    }
}
