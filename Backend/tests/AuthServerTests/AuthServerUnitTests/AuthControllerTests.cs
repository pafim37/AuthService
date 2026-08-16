using AuthServer.Authentication;
using AuthServer.Controllers;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuthServerUnitTests;

public class AuthControllerTests
{
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IRoleRepository> roleRepository = new();
    private readonly AuthController sut;
    private readonly AuthServer.Database.AuthContext authContext;
    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public AuthControllerTests()
    {
        authContext = ControllerTestHelpers.CreateContext();
        var configuration = ControllerTestHelpers.TestConfiguration();
        sut = new AuthController(
            userRepository.Object,
            roleRepository.Object,
            new JwtTokenService(configuration),
            new RefreshTokenService(authContext, configuration),
            ControllerTestHelpers.DevelopmentEnvironment().Object);

        ControllerTestHelpers.SetHttpContext(sut, ControllerTestHelpers.HttpContextWithUser());
    }

    [Fact]
    public async Task SignUp_WhenCredentialsAreInvalid_ReturnsBadRequest()
    {
        IActionResult result = await sut.SignUp(new CredentialsDto { Login = "", Password = "password" }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SignUp_WhenLoginAlreadyExists_ReturnsConflict()
    {
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync(ControllerTestHelpers.User("user"));

        IActionResult result = await sut.SignUp(new CredentialsDto { Login = "user", Password = "password" }, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task SignUp_WhenDefaultRoleDoesNotExist_ReturnsBadRequest()
    {
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync((UserEntity?)null);
        roleRepository.Setup(m => m.GetRoleByNameAsync("Default", cancellationToken)).ReturnsAsync((RoleEntity?)null);

        IActionResult result = await sut.SignUp(new CredentialsDto { Login = "user", Password = "password" }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SignUp_WhenRequestIsValid_ReturnsCreatedTokens()
    {
        RoleEntity role = ControllerTestHelpers.Role("Default");
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync((UserEntity?)null);
        roleRepository.Setup(m => m.GetRoleByNameAsync("Default", cancellationToken)).ReturnsAsync(role);

        IActionResult result = await sut.SignUp(new CredentialsDto { Login = "user", Password = "password" }, cancellationToken);

        AuthTokenDto tokens = ControllerTestHelpers.CreatedValueOf<AuthTokenDto>(result);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        userRepository.Verify(m => m.CreateUserAsync(It.Is<UserEntity>(u => u.Role == role), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SignIn_WhenCredentialsAreMissing_ReturnsBadRequest()
    {
        IActionResult result = await sut.SignIn(new SignInDto { Login = "user", Password = "" }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SignIn_WhenUserDoesNotExist_ReturnsUnauthorized()
    {
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync((UserEntity?)null);

        IActionResult result = await sut.SignIn(new SignInDto { Login = "user", Password = "password" }, cancellationToken);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task SignIn_WhenPasswordHashIsMissing_ReturnsUnauthorized()
    {
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync(ControllerTestHelpers.User("user", ""));

        IActionResult result = await sut.SignIn(new SignInDto { Login = "user", Password = "password" }, cancellationToken);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task SignIn_WhenPasswordIsInvalid_ReturnsUnauthorized()
    {
        UserEntity user = ControllerTestHelpers.User("user", BCrypt.Net.BCrypt.HashPassword("correct"));
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.SignIn(new SignInDto { Login = "user", Password = "wrong" }, cancellationToken);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task SignIn_WhenCredentialsAreValid_ReturnsTokens()
    {
        UserEntity user = ControllerTestHelpers.User("user", BCrypt.Net.BCrypt.HashPassword("password"));
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.SignIn(new SignInDto { Login = "user", Password = "password" }, cancellationToken);

        AuthTokenDto tokens = ControllerTestHelpers.OkValueOf<AuthTokenDto>(result);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
    }

    [Fact]
    public async Task Refresh_WhenTokenIsMissing_ReturnsBadRequest()
    {
        IActionResult result = await sut.Refresh(new RefreshTokenDto(), cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsInvalid_ReturnsUnauthorized()
    {
        IActionResult result = await sut.Refresh(new RefreshTokenDto { RefreshToken = "invalid" }, cancellationToken);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Refresh_WhenTokenIsValid_ReturnsNewTokens()
    {
        UserEntity user = ControllerTestHelpers.User("user");
        var refreshTokenService = new RefreshTokenService(authContext, ControllerTestHelpers.TestConfiguration());
        RefreshTokenResult refreshToken = await refreshTokenService.CreateRefreshTokenAsync(user, cancellationToken);
        await refreshTokenService.SaveChangesAsync(cancellationToken);

        IActionResult result = await sut.Refresh(new RefreshTokenDto { RefreshToken = refreshToken.Token }, cancellationToken);

        AuthTokenDto tokens = ControllerTestHelpers.OkValueOf<AuthTokenDto>(result);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.NotEqual(refreshToken.Token, tokens.RefreshToken);
    }

    [Fact]
    public async Task AdminSignIn_WhenCredentialsAreMissing_ReturnsBadRequest()
    {
        IActionResult result = await sut.AdminSignIn(new SignInDto { Login = "", Password = "password" }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AdminSignIn_WhenUserDoesNotExist_ReturnsUnauthorized()
    {
        userRepository.Setup(m => m.GetUserByLoginAsync("admin", cancellationToken)).ReturnsAsync((UserEntity?)null);

        IActionResult result = await sut.AdminSignIn(new SignInDto { Login = "admin", Password = "password" }, cancellationToken);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task AdminSignIn_WhenPasswordIsInvalid_ReturnsUnauthorized()
    {
        UserEntity user = ControllerTestHelpers.User("admin", BCrypt.Net.BCrypt.HashPassword("correct"), ControllerTestHelpers.Role("Administrator"));
        userRepository.Setup(m => m.GetUserByLoginAsync("admin", cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.AdminSignIn(new SignInDto { Login = "admin", Password = "wrong" }, cancellationToken);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task AdminSignIn_WhenUserIsNotAdministrator_ReturnsForbidden()
    {
        UserEntity user = ControllerTestHelpers.User("user", BCrypt.Net.BCrypt.HashPassword("password"), ControllerTestHelpers.Role("Default"));
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.AdminSignIn(new SignInDto { Login = "user", Password = "password" }, cancellationToken);

        var statusCode = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, statusCode.StatusCode);
    }

    [Fact]
    public async Task AdminSignIn_WhenAdministratorCredentialsAreValid_ReturnsTokens()
    {
        UserEntity user = ControllerTestHelpers.User("admin", BCrypt.Net.BCrypt.HashPassword("password"), ControllerTestHelpers.Role("Administrator"));
        userRepository.Setup(m => m.GetUserByLoginAsync("admin", cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.AdminSignIn(new SignInDto { Login = "admin", Password = "password" }, cancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(ControllerTestHelpers.OkValueOf<AuthTokenDto>(result).AccessToken));
    }

    [Fact]
    public void Me_WhenClaimIsMissing_ReturnsUnauthorized()
    {
        ControllerTestHelpers.SetHttpContext(sut, ControllerTestHelpers.HttpContextWithUser());

        IActionResult result = sut.Me();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void Me_WhenClaimExists_ReturnsLogin()
    {
        ControllerTestHelpers.SetHttpContext(sut, ControllerTestHelpers.HttpContextWithUser(login: "user"));

        IActionResult result = sut.Me();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("user", ok.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_WhenTokenIsMissing_ReturnsBadRequest()
    {
        IActionResult result = await sut.Logout(new RefreshTokenDto(), cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Logout_WhenTokenExistsWithoutStoredToken_ReturnsOk()
    {
        IActionResult result = await sut.Logout(new RefreshTokenDto { RefreshToken = "missing" }, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Logout successful.", ok.Value);
    }

    [Fact]
    public async Task Logout_WhenTokenAndUserClaimExist_RevokesTokenAndIncrementsSessionVersion()
    {
        UserEntity user = ControllerTestHelpers.User("user");
        var refreshTokenService = new RefreshTokenService(authContext, ControllerTestHelpers.TestConfiguration());
        RefreshTokenResult refreshToken = await refreshTokenService.CreateRefreshTokenAsync(user, cancellationToken);
        await refreshTokenService.SaveChangesAsync(cancellationToken);
        ControllerTestHelpers.SetHttpContext(sut, ControllerTestHelpers.HttpContextWithUser(user.Id, "user"));
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.Logout(new RefreshTokenDto { RefreshToken = refreshToken.Token }, cancellationToken);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, user.SessionVersion);
        Assert.NotNull(refreshToken.Entity.RevokedAtUtc);
        userRepository.Verify(m => m.UpdateUserAsync(user, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WhenNewPasswordIsMissing_ReturnsBadRequest()
    {
        IActionResult result = await sut.ChangePassword(new ChangePasswordDto { NewPassword = "" }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WhenUserClaimIsMissing_ReturnsUnauthorized()
    {
        ControllerTestHelpers.SetHttpContext(sut, ControllerTestHelpers.HttpContextWithUser(login: "user"));

        IActionResult result = await sut.ChangePassword(new ChangePasswordDto { NewPassword = "new-password" }, cancellationToken);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WhenUserExists_UpdatesPasswordAndIncrementsSessionVersion()
    {
        UserEntity user = ControllerTestHelpers.User("user", BCrypt.Net.BCrypt.HashPassword("old-password"));
        ControllerTestHelpers.SetHttpContext(sut, ControllerTestHelpers.HttpContextWithUser(user.Id, "user"));
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.ChangePassword(new ChangePasswordDto { NewPassword = "new-password" }, cancellationToken);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(BCrypt.Net.BCrypt.Verify("new-password", user.PasswordHashed));
        Assert.Equal(1, user.SessionVersion);
        userRepository.Verify(m => m.UpdateUserAsync(user, cancellationToken), Times.Once);
    }
}
