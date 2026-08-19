using AuthServer.Controllers;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuthServerUnitTests;

public class RoleControllerTests
{
    private readonly Mock<IRoleRepository> roleRepository = new();
    private readonly Mock<IPrivilegeRepository> privilegeRepository = new();
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly RoleController sut;
    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public RoleControllerTests()
    {
        sut = new RoleController(roleRepository.Object, privilegeRepository.Object, userRepository.Object);
    }

    [Fact]
    public async Task GetRoles_ReturnsRoles()
    {
        RoleEntity role = ControllerTestHelpers.Role("Default", ControllerTestHelpers.Privilege("Read"));
        roleRepository.Setup(m => m.GetAllRolesAsync(cancellationToken)).ReturnsAsync([role]);

        IActionResult result = await sut.GetRoles(cancellationToken);

        RoleDto dto = Assert.Single(ControllerTestHelpers.OkValueOf<IEnumerable<RoleDto>>(result));
        Assert.Equal(role.Id, dto.Id);
        Assert.Equal("Default", dto.Name);
        Assert.Equal("Read", Assert.Single(dto.Privileges).Name);
    }

    [Fact]
    public async Task GetRole_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        roleRepository.Setup(m => m.GetRoleByIdAsync(id, cancellationToken)).ReturnsAsync((RoleEntity?)null);

        IActionResult result = await sut.GetRole(id, cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetRole_WhenRoleExists_ReturnsRole()
    {
        RoleEntity role = ControllerTestHelpers.Role();
        roleRepository.Setup(m => m.GetRoleByIdAsync(role.Id, cancellationToken)).ReturnsAsync(role);

        IActionResult result = await sut.GetRole(role.Id, cancellationToken);

        Assert.Equal(role.Id, ControllerTestHelpers.OkValueOf<RoleDto>(result).Id);
    }

    [Fact]
    public async Task CreateRole_WhenNameIsMissing_ReturnsBadRequest()
    {
        IActionResult result = await sut.CreateRole(new RoleRequestDto { Name = " " }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateRole_WhenRoleAlreadyExists_ReturnsConflict()
    {
        roleRepository.Setup(m => m.GetRoleByNameAsync("Default", cancellationToken)).ReturnsAsync(ControllerTestHelpers.Role("Default"));

        IActionResult result = await sut.CreateRole(new RoleRequestDto { Name = "Default" }, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CreateRole_WhenAnyPrivilegeDoesNotExist_ReturnsBadRequest()
    {
        roleRepository.Setup(m => m.GetRoleByNameAsync("Default", cancellationToken)).ReturnsAsync((RoleEntity?)null);
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Read", cancellationToken)).ReturnsAsync(ControllerTestHelpers.Privilege("Read"));
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Missing", cancellationToken)).ReturnsAsync((PrivilegeEntity?)null);

        IActionResult result = await sut.CreateRole(new RoleRequestDto { Name = "Default", Privileges = ["Read", "Missing"] }, cancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("One or more privileges do not exist.", badRequest.Value);
    }

    [Fact]
    public async Task CreateRole_WhenRequestIsValid_ReturnsCreatedRole()
    {
        PrivilegeEntity privilege = ControllerTestHelpers.Privilege("Read");
        roleRepository.Setup(m => m.GetRoleByNameAsync("Default", cancellationToken)).ReturnsAsync((RoleEntity?)null);
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Read", cancellationToken)).ReturnsAsync(privilege);

        IActionResult result = await sut.CreateRole(new RoleRequestDto { Name = "Default", Privileges = ["Read", "read"] }, cancellationToken);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        RoleDto dto = Assert.IsType<RoleDto>(created.Value);
        Assert.Equal("Default", dto.Name);
        Assert.Equal("Read", Assert.Single(dto.Privileges).Name);
        roleRepository.Verify(m => m.CreateRoleAsync(It.Is<RoleEntity>(r => r.Privileges.Single() == privilege), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateRole_WhenNameIsMissing_ReturnsBadRequest()
    {
        IActionResult result = await sut.UpdateRole(Guid.NewGuid(), new RoleRequestDto { Name = "" }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRole_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        roleRepository.Setup(m => m.GetRoleByIdAsync(id, cancellationToken)).ReturnsAsync((RoleEntity?)null);

        IActionResult result = await sut.UpdateRole(id, new RoleRequestDto { Name = "User" }, cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRole_WhenNameBelongsToAnotherRole_ReturnsConflict()
    {
        Guid id = Guid.NewGuid();
        roleRepository.Setup(m => m.GetRoleByIdAsync(id, cancellationToken)).ReturnsAsync(ControllerTestHelpers.Role("Default"));
        roleRepository.Setup(m => m.GetRoleByNameAsync("User", cancellationToken)).ReturnsAsync(ControllerTestHelpers.Role("User"));

        IActionResult result = await sut.UpdateRole(id, new RoleRequestDto { Name = "User" }, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRole_WhenAnyPrivilegeDoesNotExist_ReturnsBadRequest()
    {
        RoleEntity role = ControllerTestHelpers.Role("Default");
        roleRepository.Setup(m => m.GetRoleByIdAsync(role.Id, cancellationToken)).ReturnsAsync(role);
        roleRepository.Setup(m => m.GetRoleByNameAsync("User", cancellationToken)).ReturnsAsync((RoleEntity?)null);

        IActionResult result = await sut.UpdateRole(role.Id, new RoleRequestDto { Name = "User", Privileges = ["Missing"] }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateRole_WhenRequestIsValid_ReturnsUpdatedRole()
    {
        PrivilegeEntity privilege = ControllerTestHelpers.Privilege("Write");
        RoleEntity role = ControllerTestHelpers.Role("Default", ControllerTestHelpers.Privilege("Read"));
        roleRepository.Setup(m => m.GetRoleByIdAsync(role.Id, cancellationToken)).ReturnsAsync(role);
        roleRepository.Setup(m => m.GetRoleByNameAsync("User", cancellationToken)).ReturnsAsync((RoleEntity?)null);
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Write", cancellationToken)).ReturnsAsync(privilege);

        IActionResult result = await sut.UpdateRole(role.Id, new RoleRequestDto { Name = "User", Privileges = ["Write"] }, cancellationToken);

        RoleDto dto = ControllerTestHelpers.OkValueOf<RoleDto>(result);
        Assert.Equal("User", dto.Name);
        Assert.Equal("Write", Assert.Single(dto.Privileges).Name);
        roleRepository.Verify(m => m.UpdateRoleAsync(role, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PatchRole_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        roleRepository.Setup(m => m.GetRoleByIdAsync(id, cancellationToken)).ReturnsAsync((RoleEntity?)null);

        IActionResult result = await sut.PatchRole(id, new RolePatchDto(), cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task PatchRole_WhenNameBelongsToAnotherRole_ReturnsConflict()
    {
        Guid id = Guid.NewGuid();
        roleRepository.Setup(m => m.GetRoleByIdAsync(id, cancellationToken)).ReturnsAsync(ControllerTestHelpers.Role("Default"));
        roleRepository.Setup(m => m.GetRoleByNameAsync("User", cancellationToken)).ReturnsAsync(ControllerTestHelpers.Role("User"));

        IActionResult result = await sut.PatchRole(id, new RolePatchDto { Name = "User" }, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task PatchRole_WhenAnyPrivilegeDoesNotExist_ReturnsBadRequest()
    {
        RoleEntity role = ControllerTestHelpers.Role("Default");
        roleRepository.Setup(m => m.GetRoleByIdAsync(role.Id, cancellationToken)).ReturnsAsync(role);

        IActionResult result = await sut.PatchRole(role.Id, new RolePatchDto { Privileges = ["Missing"] }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PatchRole_WhenRequestIsValid_ReturnsUpdatedRole()
    {
        PrivilegeEntity privilege = ControllerTestHelpers.Privilege("Write");
        RoleEntity role = ControllerTestHelpers.Role("Default", ControllerTestHelpers.Privilege("Read"));
        roleRepository.Setup(m => m.GetRoleByIdAsync(role.Id, cancellationToken)).ReturnsAsync(role);
        roleRepository.Setup(m => m.GetRoleByNameAsync("User", cancellationToken)).ReturnsAsync((RoleEntity?)null);
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Write", cancellationToken)).ReturnsAsync(privilege);

        IActionResult result = await sut.PatchRole(role.Id, new RolePatchDto { Name = "User", Privileges = ["Write"] }, cancellationToken);

        RoleDto dto = ControllerTestHelpers.OkValueOf<RoleDto>(result);
        Assert.Equal("User", dto.Name);
        Assert.Equal("Write", Assert.Single(dto.Privileges).Name);
    }

    [Fact]
    public async Task DeleteRole_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        roleRepository.Setup(m => m.GetRoleByIdAsync(id, cancellationToken)).ReturnsAsync((RoleEntity?)null);

        IActionResult result = await sut.DeleteRole(id, cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRole_WhenRoleIsProtected_ReturnsConflict()
    {
        RoleEntity role = ControllerTestHelpers.Role("administrator");
        roleRepository.Setup(m => m.GetRoleByIdAsync(role.Id, cancellationToken)).ReturnsAsync(role);

        IActionResult result = await sut.DeleteRole(role.Id, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task DeleteRole_WhenRoleCanBeDeleted_ReturnsNoContent()
    {
        RoleEntity role = ControllerTestHelpers.Role("Default");
        roleRepository.Setup(m => m.GetRoleByIdAsync(role.Id, cancellationToken)).ReturnsAsync(role);

        IActionResult result = await sut.DeleteRole(role.Id, cancellationToken);

        Assert.IsType<NoContentResult>(result);
        roleRepository.Verify(m => m.RemoveRoleAsync(role, cancellationToken), Times.Once);
    }
}
