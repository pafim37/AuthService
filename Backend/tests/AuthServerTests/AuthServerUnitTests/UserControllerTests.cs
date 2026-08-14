using AuthServer.Controllers;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuthServerUnitTests;

public class UserControllerTests
{
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly Mock<IRoleRepository> roleRepository = new();
    private readonly UserController sut;
    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public UserControllerTests()
    {
        sut = new UserController(userRepository.Object, roleRepository.Object);
    }

    [Fact]
    public async Task GetUsers_ReturnsUsers()
    {
        UserEntity user = ControllerTestHelpers.User(role: ControllerTestHelpers.Role("Default"));
        userRepository.Setup(m => m.GetAllUsersAsync(cancellationToken)).ReturnsAsync([user]);

        IActionResult result = await sut.GetUsers(cancellationToken);

        UserDto dto = Assert.Single(ControllerTestHelpers.OkValueOf<IEnumerable<UserDto>>(result));
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Login, dto.Login);
        Assert.Equal("Default", dto.Role!.Name);
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        userRepository.Setup(m => m.GetUserByIdAsync(id, cancellationToken)).ReturnsAsync((UserEntity?)null);

        IActionResult result = await sut.GetUser(id, cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ReturnsUser()
    {
        UserEntity user = ControllerTestHelpers.User();
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.GetUser(user.Id, cancellationToken);

        Assert.Equal(user.Id, ControllerTestHelpers.OkValueOf<UserDto>(result).Id);
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("login", null)]
    [InlineData(" ", "password")]
    public async Task CreateNewUser_WhenCredentialsAreInvalid_ReturnsBadRequest(string? login, string? password)
    {
        IActionResult result = await sut.CreateNewUser(new CredentialsDto { Login = login, Password = password }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateNewUser_WhenLoginAlreadyExists_ReturnsConflict()
    {
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync(ControllerTestHelpers.User("user"));

        IActionResult result = await sut.CreateNewUser(new CredentialsDto { Login = "user", Password = "password" }, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CreateNewUser_WhenDefaultRoleDoesNotExist_ReturnsBadRequest()
    {
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync((UserEntity?)null);
        roleRepository.Setup(m => m.GetRoleByNameAsync("Default", cancellationToken)).ReturnsAsync((RoleEntity?)null);

        IActionResult result = await sut.CreateNewUser(new CredentialsDto { Login = "user", Password = "password" }, cancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Role with name 'Default' not found.", badRequest.Value);
    }

    [Fact]
    public async Task CreateNewUser_WhenRequestIsValid_ReturnsCreatedUser()
    {
        RoleEntity role = ControllerTestHelpers.Role("Default");
        userRepository.Setup(m => m.GetUserByLoginAsync("user", cancellationToken)).ReturnsAsync((UserEntity?)null);
        roleRepository.Setup(m => m.GetRoleByNameAsync("Default", cancellationToken)).ReturnsAsync(role);

        IActionResult result = await sut.CreateNewUser(new CredentialsDto { Login = "user", Password = "password" }, cancellationToken);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        UserDto dto = Assert.IsType<UserDto>(created.Value);
        Assert.Equal("user", dto.Login);
        Assert.Equal("Default", dto.Role!.Name);
        userRepository.Verify(m => m.CreateUserAsync(It.Is<UserEntity>(u => u.PasswordHashed != "password" && u.Role == role), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateAdmin_WhenAdministratorRoleExists_ReturnsCreatedAdmin()
    {
        RoleEntity role = ControllerTestHelpers.Role("Administrator");
        userRepository.Setup(m => m.GetUserByLoginAsync("admin2", cancellationToken)).ReturnsAsync((UserEntity?)null);
        roleRepository.Setup(m => m.GetRoleByNameAsync("Administrator", cancellationToken)).ReturnsAsync(role);

        IActionResult result = await sut.CreateAdmin(new CredentialsDto { Login = "admin2", Password = "password" }, cancellationToken);

        UserDto dto = ControllerTestHelpers.CreatedValueOf<UserDto>(result);
        Assert.Equal("admin2", dto.Login);
        Assert.Equal("Administrator", dto.Role!.Name);
    }

    [Fact]
    public async Task UpdateUser_WhenCredentialsAreInvalid_ReturnsBadRequest()
    {
        IActionResult result = await sut.UpdateUser(Guid.NewGuid(), new CredentialsDto { Login = "", Password = "password" }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        userRepository.Setup(m => m.GetUserByIdAsync(id, cancellationToken)).ReturnsAsync((UserEntity?)null);

        IActionResult result = await sut.UpdateUser(id, new CredentialsDto { Login = "user", Password = "password" }, cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUser_WhenLoginBelongsToAnotherUser_ReturnsConflict()
    {
        UserEntity user = ControllerTestHelpers.User("user");
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);
        userRepository.Setup(m => m.GetUserByLoginAsync("other", cancellationToken)).ReturnsAsync(ControllerTestHelpers.User("other"));

        IActionResult result = await sut.UpdateUser(user.Id, new CredentialsDto { Login = "other", Password = "password" }, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UpdateUser_WhenRequestIsValid_ReturnsUpdatedUser()
    {
        UserEntity user = ControllerTestHelpers.User("user");
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);
        userRepository.Setup(m => m.GetUserByLoginAsync("updated", cancellationToken)).ReturnsAsync((UserEntity?)null);

        IActionResult result = await sut.UpdateUser(user.Id, new CredentialsDto { Login = "updated", Password = "newPassword" }, cancellationToken);

        UserDto dto = ControllerTestHelpers.OkValueOf<UserDto>(result);
        Assert.Equal("updated", dto.Login);
        Assert.NotEqual("newPassword", user.PasswordHashed);
        userRepository.Verify(m => m.UpdateUserAsync(user, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PatchUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        userRepository.Setup(m => m.GetUserByIdAsync(id, cancellationToken)).ReturnsAsync((UserEntity?)null);

        IActionResult result = await sut.PatchUser(id, new UserPatchDto(), cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task PatchUser_WhenLoginBelongsToAnotherUser_ReturnsConflict()
    {
        UserEntity user = ControllerTestHelpers.User("user");
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);
        userRepository.Setup(m => m.GetUserByLoginAsync("other", cancellationToken)).ReturnsAsync(ControllerTestHelpers.User("other"));

        IActionResult result = await sut.PatchUser(user.Id, new UserPatchDto { Login = "other" }, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task PatchUser_WhenRoleDoesNotExist_ReturnsBadRequest()
    {
        UserEntity user = ControllerTestHelpers.User("user");
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);
        roleRepository.Setup(m => m.GetRoleByNameAsync("Missing", cancellationToken)).ReturnsAsync((RoleEntity?)null);

        IActionResult result = await sut.PatchUser(user.Id, new UserPatchDto { Role = "Missing" }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PatchUser_WhenRequestIsValid_ReturnsUpdatedUser()
    {
        RoleEntity role = ControllerTestHelpers.Role("Administrator");
        UserEntity user = ControllerTestHelpers.User("user");
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);
        userRepository.Setup(m => m.GetUserByLoginAsync("updated", cancellationToken)).ReturnsAsync((UserEntity?)null);
        roleRepository.Setup(m => m.GetRoleByNameAsync("Administrator", cancellationToken)).ReturnsAsync(role);

        IActionResult result = await sut.PatchUser(user.Id, new UserPatchDto { Login = "updated", Password = "newPassword", Role = "Administrator" }, cancellationToken);

        UserDto dto = ControllerTestHelpers.OkValueOf<UserDto>(result);
        Assert.Equal("updated", dto.Login);
        Assert.Equal("Administrator", dto.Role!.Name);
        Assert.NotEqual("newPassword", user.PasswordHashed);
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        userRepository.Setup(m => m.GetUserByIdAsync(id, cancellationToken)).ReturnsAsync((UserEntity?)null);

        IActionResult result = await sut.DeleteUser(id, cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_WhenUserIsProtected_ReturnsConflict()
    {
        UserEntity user = ControllerTestHelpers.User("admin");
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.DeleteUser(user.Id, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task DeleteUser_WhenUserCanBeDeleted_ReturnsNoContent()
    {
        UserEntity user = ControllerTestHelpers.User("user");
        userRepository.Setup(m => m.GetUserByIdAsync(user.Id, cancellationToken)).ReturnsAsync(user);

        IActionResult result = await sut.DeleteUser(user.Id, cancellationToken);

        Assert.IsType<NoContentResult>(result);
        userRepository.Verify(m => m.RemoveUserAsync(user, cancellationToken), Times.Once);
    }
}
