using AuthServer.Controllers;
using AuthServer.Database.Models;
using AuthServer.Database.Repositories;
using AuthServer.DataTransferObjects;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AuthServerUnitTests;

public class PrivilegeControllerTests
{
    private readonly Mock<IPrivilegeRepository> privilegeRepository = new();
    private readonly Mock<IUserRepository> userRepository = new();
    private readonly PrivilegeController sut;
    private readonly CancellationToken cancellationToken = CancellationToken.None;

    public PrivilegeControllerTests()
    {
        sut = new PrivilegeController(privilegeRepository.Object, userRepository.Object);
    }

    [Fact]
    public async Task PrivilegeController_GetPrivileges_ReturnsPrivileges()
    {
        PrivilegeEntity p1 = ControllerTestHelpers.Privilege("Privilege1", "Description1");
        PrivilegeEntity p2 = ControllerTestHelpers.Privilege("Privilege2", "Description2");
        privilegeRepository.Setup(m => m.GetAllPrivilegesAsync(cancellationToken)).ReturnsAsync([p1, p2]);

        IActionResult result = await sut.GetPrivileges(cancellationToken);

        var privileges = ControllerTestHelpers.OkValueOf<IEnumerable<PrivilegeDto>>(result).ToList();
        Assert.Equal([p1.Id, p2.Id], privileges.Select(p => p.Id));
        Assert.Equal(["Privilege1", "Privilege2"], privileges.Select(p => p.Name));
    }

    [Fact]
    public async Task GetPrivilege_WhenPrivilegeDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(id, cancellationToken)).ReturnsAsync((PrivilegeEntity?)null);

        IActionResult result = await sut.GetPrivilege(id, cancellationToken);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Privilege with id '{id}' not found.", notFound.Value);
    }

    [Fact]
    public async Task GetPrivilege_WhenPrivilegeExists_ReturnsPrivilege()
    {
        PrivilegeEntity privilege = ControllerTestHelpers.Privilege();
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(privilege.Id, cancellationToken)).ReturnsAsync(privilege);

        IActionResult result = await sut.GetPrivilege(privilege.Id, cancellationToken);

        PrivilegeDto dto = ControllerTestHelpers.OkValueOf<PrivilegeDto>(result);
        Assert.Equal(privilege.Id, dto.Id);
        Assert.Equal(privilege.Name, dto.Name);
        Assert.Equal(privilege.Description, dto.Description);
    }

    [Fact]
    public async Task CreateNewPrivilege_WhenNameIsMissing_ReturnsBadRequest()
    {
        IActionResult result = await sut.CreateNewPrivilege(new PrivilegeRequestDto { Name = " " }, cancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Privilege name is required.", badRequest.Value);
        privilegeRepository.Verify(m => m.CreatePrivilegeAsync(It.IsAny<PrivilegeEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateNewPrivilege_WhenNameAlreadyExists_ReturnsConflict()
    {
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Read", cancellationToken)).ReturnsAsync(ControllerTestHelpers.Privilege("Read"));

        IActionResult result = await sut.CreateNewPrivilege(new PrivilegeRequestDto { Name = "Read" }, cancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Privilege with name 'Read' already exists.", conflict.Value);
    }

    [Fact]
    public async Task CreateNewPrivilege_WhenRequestIsValid_ReturnsCreatedPrivilege()
    {
        PrivilegeEntity? createdPrivilege = null;
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Read", cancellationToken)).ReturnsAsync((PrivilegeEntity?)null);
        privilegeRepository
            .Setup(m => m.CreatePrivilegeAsync(It.IsAny<PrivilegeEntity>(), cancellationToken))
            .Callback<PrivilegeEntity, CancellationToken>((privilege, _) => createdPrivilege = privilege)
            .Returns(Task.CompletedTask);

        IActionResult result = await sut.CreateNewPrivilege(new PrivilegeRequestDto { Name = "Read", Description = "Can read" }, cancellationToken);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(PrivilegeController.GetPrivilege), created.ActionName);
        PrivilegeDto dto = Assert.IsType<PrivilegeDto>(created.Value);
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("Read", dto.Name);
        Assert.Equal("Can read", dto.Description);
        Assert.Same(createdPrivilege, Assert.Single(privilegeRepository.Invocations, i => i.Method.Name == nameof(IPrivilegeRepository.CreatePrivilegeAsync)).Arguments[0]);
    }

    [Fact]
    public async Task UpdatePrivilege_WhenNameIsMissing_ReturnsBadRequest()
    {
        IActionResult result = await sut.UpdatePrivilege(Guid.NewGuid(), new PrivilegeRequestDto { Name = "" }, cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePrivilege_WhenPrivilegeDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(id, cancellationToken)).ReturnsAsync((PrivilegeEntity?)null);

        IActionResult result = await sut.UpdatePrivilege(id, new PrivilegeRequestDto { Name = "Write" }, cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePrivilege_WhenNameBelongsToAnotherPrivilege_ReturnsConflict()
    {
        Guid id = Guid.NewGuid();
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(id, cancellationToken)).ReturnsAsync(ControllerTestHelpers.Privilege("Read"));
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Write", cancellationToken)).ReturnsAsync(ControllerTestHelpers.Privilege("Write"));

        IActionResult result = await sut.UpdatePrivilege(id, new PrivilegeRequestDto { Name = "Write" }, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task UpdatePrivilege_WhenRequestIsValid_ReturnsUpdatedPrivilege()
    {
        PrivilegeEntity privilege = ControllerTestHelpers.Privilege("Read");
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(privilege.Id, cancellationToken)).ReturnsAsync(privilege);
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Write", cancellationToken)).ReturnsAsync((PrivilegeEntity?)null);

        IActionResult result = await sut.UpdatePrivilege(privilege.Id, new PrivilegeRequestDto { Name = "Write" }, cancellationToken);

        PrivilegeDto dto = ControllerTestHelpers.OkValueOf<PrivilegeDto>(result);
        Assert.Equal("Write", dto.Name);
        privilegeRepository.Verify(m => m.UpdatePrivilegeAsync(privilege, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PatchPrivilege_WhenPrivilegeDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(id, cancellationToken)).ReturnsAsync((PrivilegeEntity?)null);

        IActionResult result = await sut.PatchPrivilege(id, new PrivilegeRequestDto(), cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task PatchPrivilege_WhenNameBelongsToAnotherPrivilege_ReturnsConflict()
    {
        Guid id = Guid.NewGuid();
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(id, cancellationToken)).ReturnsAsync(ControllerTestHelpers.Privilege("Read"));
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Write", cancellationToken)).ReturnsAsync(ControllerTestHelpers.Privilege("Write"));

        IActionResult result = await sut.PatchPrivilege(id, new PrivilegeRequestDto { Name = "Write" }, cancellationToken);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task PatchPrivilege_WhenRequestIsValid_ReturnsUpdatedPrivilege()
    {
        PrivilegeEntity privilege = ControllerTestHelpers.Privilege("Read", "Old");
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(privilege.Id, cancellationToken)).ReturnsAsync(privilege);
        privilegeRepository.Setup(m => m.GetPrivilegeByNameAsync("Write", cancellationToken)).ReturnsAsync((PrivilegeEntity?)null);

        IActionResult result = await sut.PatchPrivilege(privilege.Id, new PrivilegeRequestDto { Name = "Write", Description = "New" }, cancellationToken);

        PrivilegeDto dto = ControllerTestHelpers.OkValueOf<PrivilegeDto>(result);
        Assert.Equal("Write", dto.Name);
        Assert.Equal("New", dto.Description);
        privilegeRepository.Verify(m => m.UpdatePrivilegeAsync(privilege, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task DeletePrivilege_WhenPrivilegeDoesNotExist_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(id, cancellationToken)).ReturnsAsync((PrivilegeEntity?)null);

        IActionResult result = await sut.DeletePrivilege(id, cancellationToken);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePrivilege_WhenPrivilegeIsProtected_ReturnsConflict()
    {
        PrivilegeEntity privilege = ControllerTestHelpers.Privilege("Full");
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(privilege.Id, cancellationToken)).ReturnsAsync(privilege);

        IActionResult result = await sut.DeletePrivilege(privilege.Id, cancellationToken);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal("Built-in Full privilege cannot be deleted.", conflict.Value);
    }

    [Fact]
    public async Task DeletePrivilege_WhenPrivilegeCanBeDeleted_ReturnsNoContent()
    {
        PrivilegeEntity privilege = ControllerTestHelpers.Privilege("Read");
        privilegeRepository.Setup(m => m.GetPrivilegeByIdAsync(privilege.Id, cancellationToken)).ReturnsAsync(privilege);

        IActionResult result = await sut.DeletePrivilege(privilege.Id, cancellationToken);

        Assert.IsType<NoContentResult>(result);
        privilegeRepository.Verify(m => m.RemovePrivilegeAsync(privilege, cancellationToken), Times.Once);
    }
}

