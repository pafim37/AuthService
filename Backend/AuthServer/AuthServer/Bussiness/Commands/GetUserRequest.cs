using AuthServer.Database.Models;
using MediatR;

namespace AuthServer.Bussiness.Commands
{
    public record GetUserRequest(string Login) : IRequest<UserEntity> { }
}
