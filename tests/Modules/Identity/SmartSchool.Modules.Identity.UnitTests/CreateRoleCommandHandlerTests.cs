using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class CreateRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesRoleSuccessfully()
    {
        var repository = new FakeRoleRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repository, unitOfWork);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedRole);
        Assert.Equal(result.RoleId, repository.AddedRole.RoleId);
        Assert.Equal("TEACHER", repository.LastNormalizedNameChecked);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_RejectsDuplicateRoleWithinSameSchool()
    {
        var command = ValidCommand();
        var repository = new FakeRoleRepository((command.SchoolId, "TEACHER"));
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("identity.role_name_exists", result.ErrorCode);
        Assert.Null(repository.AddedRole);
    }

    [Fact]
    public async Task Handle_AllowsSameRoleNameInAnotherSchool()
    {
        var otherSchoolId = Guid.NewGuid();
        var repository = new FakeRoleRepository((otherSchoolId, "TEACHER"));
        var handler = CreateHandler(repository, new FakeUnitOfWork());

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.AddedRole);
    }

    private static CreateRoleCommandHandler CreateHandler(FakeRoleRepository repository, FakeUnitOfWork unitOfWork) =>
        new(new CreateRoleCommandValidator(), repository, unitOfWork);

    private static CreateRoleCommand ValidCommand() =>
        new(Guid.NewGuid(), "Teacher", "School teacher role");

    private sealed class FakeRoleRepository(params (Guid SchoolId, string NormalizedName)[] existingRoles)
        : IRoleRepository
    {
        public Role? AddedRole { get; private set; }
        public string? LastNormalizedNameChecked { get; private set; }

        public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult<Role?>(null);

        public Task<bool> NameExistsAsync(
            Guid schoolId,
            string normalizedName,
            CancellationToken cancellationToken)
        {
            LastNormalizedNameChecked = normalizedName;
            return Task.FromResult(existingRoles.Contains((schoolId, normalizedName)));
        }

        public void Add(Role role) => AddedRole = role;
    }

    private sealed class FakeUnitOfWork : IIdentityUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
