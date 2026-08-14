using AuthServer.Controllers;
using AuthServer.Database;
using AuthServer.Database.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Security.Claims;

namespace AuthServerUnitTests;

internal static class ControllerTestHelpers
{
    internal static T ValueOf<T>(IActionResult result)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        return Assert.IsAssignableFrom<T>(objectResult.Value);
    }

    internal static T OkValueOf<T>(IActionResult result)
    {
        var okResult = Assert.IsType<OkObjectResult>(result);
        return Assert.IsAssignableFrom<T>(okResult.Value);
    }

    internal static T CreatedValueOf<T>(IActionResult result)
    {
        var createdResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.True(createdResult is CreatedAtActionResult or CreatedResult);
        return Assert.IsAssignableFrom<T>(createdResult.Value);
    }

    internal static DefaultHttpContext HttpContextWithUser(Guid? userId = null, string? login = null, bool https = false)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = https ? "https" : "http";

        List<Claim> claims = [];
        if (userId is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(login))
        {
            claims.Add(new Claim(ClaimTypes.Name, login));
        }

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        return context;
    }

    internal static void SetHttpContext(ControllerBase controller, HttpContext context)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }

    internal static IConfiguration TestConfiguration()
    {
        Dictionary<string, string?> values = new()
        {
            ["Jwt:AuthServiceKey"] = "test-auth-service-signing-key-long-enough",
            ["Jwt:Issuer"] = "AuthServerUnitTests",
            ["Jwt:Audience"] = "AuthServerUnitTests",
            ["Jwt:expiresInMinutes"] = "60",
            ["Jwt:refreshTokenExpiresInDays"] = "7"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    internal static Mock<IWebHostEnvironment> DevelopmentEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);
        return environment;
    }

    internal static AuthContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AuthContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AuthContext(options);
    }

    internal static PrivilegeEntity Privilege(string name = "Read", string? description = "Description")
    {
        return new PrivilegeEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description
        };
    }

    internal static RoleEntity Role(string name = "Default", params PrivilegeEntity[] privileges)
    {
        return new RoleEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            Privileges = privileges.ToList()
        };
    }

    internal static UserEntity User(string login = "user", string passwordHash = "hash", RoleEntity? role = null)
    {
        RoleEntity userRole = role ?? Role();
        return new UserEntity
        {
            Id = Guid.NewGuid(),
            Login = login,
            PasswordHashed = passwordHash,
            RoleId = userRole.Id,
            Role = userRole
        };
    }
}
