using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class AssignRoleToUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_AssignsExistingRoleToExistingUser()
    {
        var schoolId = Guid.NewGuid();
        var user = CreateUser(schoolId);
        var role = Role.Create(schoolId, "Teacher");
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(user, role, unitOfWork);

        var result = await handler.HandleAsync(
            new AssignRoleToUserCommand(user.UserId, role.RoleId),
            CancellationToken.None);

        Assert.Equal(AssignRoleToUserStatus.Success, result.Status);
        Assert.Contains(user.UserRoles, userRole => userRole.RoleId == role.RoleId);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenUserDoesNotExist()
    {
        var role = Role.Create(Guid.NewGuid(), "Teacher");
        var handler = CreateHandler(user: null, role, new FakeUnitOfWork());

        var result = await handler.HandleAsync(
            new AssignRoleToUserCommand(Guid.NewGuid(), role.RoleId),
            CancellationToken.None);

        Assert.Equal(AssignRoleToUserStatus.NotFound, result.Status);
        Assert.Equal("identity.user_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenRoleDoesNotExist()
    {
        var user = CreateUser(Guid.NewGuid());
        var handler = CreateHandler(user, role: null, new FakeUnitOfWork());

        var result = await handler.HandleAsync(
            new AssignRoleToUserCommand(user.UserId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(AssignRoleToUserStatus.NotFound, result.Status);
        Assert.Equal("identity.role_not_found", result.ErrorCode);
    }

    [Fact]
    public async Task Handle_PreventsCrossSchoolAssignment()
    {
        var user = CreateUser(Guid.NewGuid());
        var role = Role.Create(Guid.NewGuid(), "Teacher");
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(user, role, unitOfWork);

        var result = await handler.HandleAsync(
            new AssignRoleToUserCommand(user.UserId, role.RoleId),
            CancellationToken.None);

        Assert.Equal(AssignRoleToUserStatus.Conflict, result.Status);
        Assert.Equal("identity.role_school_mismatch", result.ErrorCode);
        Assert.Empty(user.UserRoles);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_PreventsDuplicateRole()
    {
        var schoolId = Guid.NewGuid();
        var user = CreateUser(schoolId);
        var role = Role.Create(schoolId, "Teacher");
        user.AssignRole(role.RoleId);
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(user, role, unitOfWork);

        var result = await handler.HandleAsync(
            new AssignRoleToUserCommand(user.UserId, role.RoleId),
            CancellationToken.None);

        Assert.Equal(AssignRoleToUserStatus.Conflict, result.Status);
        Assert.Equal("identity.role_already_assigned", result.ErrorCode);
        Assert.Single(user.UserRoles);
        Assert.Equal(0, unitOfWork.SaveCount);
    }

    private static AssignRoleToUserCommandHandler CreateHandler(
        User? user,
        Role? role,
        FakeUnitOfWork unitOfWork) =>
        new(
            new AssignRoleToUserCommandValidator(),
            new FakeUserRepository(user),
            new FakeRoleRepository(role),
            unitOfWork);

    private static User CreateUser(Guid schoolId) => User.Create(
        schoolId,
        "admin",
        "ADMIN",
        "admin@smartschool.com",
        "ADMIN@SMARTSCHOOL.COM",
        "hashed-password",
        "Ahmed",
        "Mohamed");

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(user?.UserId == userId ? user : null);

        public Task<bool> UsernameExistsAsync(
            Guid schoolId,
            string normalizedUsername,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> EmailExistsAsync(
            Guid schoolId,
            string normalizedEmail,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public void Add(User addedUser) => throw new NotSupportedException();
    }

    private sealed class FakeRoleRepository(Role? role) : IRoleRepository
    {
        public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken) =>
            Task.FromResult(role?.RoleId == roleId ? role : null);

        public Task<bool> NameExistsAsync(
            Guid schoolId,
            string normalizedName,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public void Add(Role addedRole) => throw new NotSupportedException();
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
