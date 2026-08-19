using AuthServer.Authentication;
using AuthServer.DataTransferObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;

namespace AuthServerUnitTests;

public class LoginLockoutFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_WhenLoginFailsFiveTimes_LocksOutNextAttempt()
    {
        LoginLockoutFilter sut = CreateFilter();
        SignInDto signInDto = new() { Login = "user", Password = "wrong" };

        for (int i = 0; i < 5; i++)
        {
            ActionExecutingContext context = CreateExecutingContext(signInDto);
            await sut.OnActionExecutionAsync(context, () => CreateExecutedContext(context, new UnauthorizedObjectResult("Invalid login or password.")));
        }

        ActionExecutingContext lockedContext = CreateExecutingContext(signInDto);
        await sut.OnActionExecutionAsync(lockedContext, () => CreateExecutedContext(lockedContext, new OkResult()));

        ObjectResult result = Assert.IsType<ObjectResult>(lockedContext.Result);
        Assert.Equal(StatusCodes.Status423Locked, result.StatusCode);
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenLoginSucceeds_ResetsFailedAttempts()
    {
        LoginLockoutFilter sut = CreateFilter();
        SignInDto signInDto = new() { Login = "user", Password = "password" };

        for (int i = 0; i < 4; i++)
        {
            ActionExecutingContext failedContext = CreateExecutingContext(signInDto);
            await sut.OnActionExecutionAsync(failedContext, () => CreateExecutedContext(failedContext, new UnauthorizedResult()));
        }

        ActionExecutingContext successContext = CreateExecutingContext(signInDto);
        await sut.OnActionExecutionAsync(successContext, () => CreateExecutedContext(successContext, new OkResult()));

        for (int i = 0; i < 4; i++)
        {
            ActionExecutingContext failedContext = CreateExecutingContext(signInDto);
            await sut.OnActionExecutionAsync(failedContext, () => CreateExecutedContext(failedContext, new UnauthorizedResult()));
            Assert.Null(failedContext.Result);
        }
    }

    private static LoginLockoutFilter CreateFilter()
    {
        return new(new LoginLockoutService(new MemoryCache(new MemoryCacheOptions())));
    }

    private static ActionExecutingContext CreateExecutingContext(SignInDto signInDto)
    {
        ActionContext actionContext = new(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { ["signInDto"] = signInDto },
            controller: new object());
    }

    private static Task<ActionExecutedContext> CreateExecutedContext(ActionExecutingContext executingContext, IActionResult result)
    {
        return Task.FromResult(new ActionExecutedContext(
            executingContext,
            [],
            controller: new object())
        {
            Result = result
        });
    }
}
