using AuthServer.Bussiness.Commands;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UserController(IMediator mediator) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> CreateNewUser([FromBody] NewUserDto newUserDto)
        {
            bool isNewUserValid = ValidateNewUser(newUserDto);
            if (isNewUserValid) 
            {
                // TODO: Validate the role here or assign a default role

                // TODO: Should I convert the password here from string to byte[]
                // byte[] passwordAsByteArray = Encoding.UTF8.GetBytes(newUserDto.Password!);
                // newUserDto.Password = string.Empty;
                await mediator.Send(new CreateNewUserRequest(newUserDto.Login!, newUserDto.Password!, newUserDto.Role!)).ConfigureAwait(false);
                return Created();
            }
            else
            {
                return BadRequest("Invalid user data. Please provide valid login, password, and role.");
            }
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetUser(string name, CancellationToken cancellationToken)
        {
            UserEntity? user = await mediator.Send(new GetUserRequest(name), cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return NotFound($"User with login '{name}' not found.");
            }

            return Ok(user);
        }

        private static bool ValidateNewUser(NewUserDto newUserDto)
        {
            return !string.IsNullOrEmpty(newUserDto.Login) && !string.IsNullOrEmpty(newUserDto.Password);
        }
    }
}
