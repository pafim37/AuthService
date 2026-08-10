using AuthServer.DataTransferObjects;
using MediatR;

namespace AuthServer.Bussiness.Commands
{
    public record CreateNewUserRequest(string Login, string Password, string Role) : IRequest { }
}
