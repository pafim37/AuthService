using AuthServer.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthServer.Authentication
{
    public class LoginLockoutFilter(LoginLockoutService loginLockoutService) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            SignInDto? signInDto = context.ActionArguments.Values.OfType<SignInDto>().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signInDto?.Login))
            {
                await next().ConfigureAwait(false);
                return;
            }

            string? ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            if (loginLockoutService.IsLockedOut(signInDto.Login, ipAddress))
            {
                context.Result = new ObjectResult("Too many failed login attempts. Please try again later.")
                {
                    StatusCode = StatusCodes.Status423Locked
                };
                return;
            }

            ActionExecutedContext executedContext = await next().ConfigureAwait(false);
            int? statusCode = GetStatusCode(executedContext.Result);

            if (statusCode is StatusCodes.Status200OK or StatusCodes.Status201Created)
            {
                loginLockoutService.Reset(signInDto.Login, ipAddress);
            }
            else if (statusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
            {
                loginLockoutService.RecordFailedAttempt(signInDto.Login, ipAddress);
            }
        }

        private static int? GetStatusCode(IActionResult? result)
        {
            return result switch
            {
                ObjectResult objectResult => objectResult.StatusCode,
                StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
                _ => null
            };
        }
    }
}
