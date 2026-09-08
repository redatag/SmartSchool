using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class UserTests
{
    [Fact]
    public void Create_CreatesActiveUserWithVersion7Id()
    {
        var user = CreateUser();

        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(7, user.UserId.Version);
        Assert.NotEqual(Guid.Empty, user.UserId);
    }

    [Fact]
    public void Create_RaisesUserCreatedDomainEvent()
    {
        var user = CreateUser();

        var domainEvent = Assert.IsType<UserCreatedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.UserId, domainEvent.UserId);
        Assert.Equal(user.SchoolId, domainEvent.SchoolId);
    }

    [Fact]
    public void AssignRole_AssignsRoleSuccessfully()
    {
        var user = CreateUser();

        user.AssignRole(Guid.NewGuid());

        Assert.Single(user.UserRoles);
    }

    [Fact]
    public void AssignRole_CreatesUserRole()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId);

        var userRole = Assert.Single(user.UserRoles);
        Assert.Equal(user.UserId, userRole.UserId);
        Assert.Equal(roleId, userRole.RoleId);
        Assert.Null(userRole.AssignedBy);
        Assert.True(userRole.AssignedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void AssignRole_RaisesRoleAssignedDomainEvent()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();
        user.ClearDomainEvents();

        user.AssignRole(roleId);

        var domainEvent = Assert.IsType<RoleAssignedDomainEvent>(Assert.Single(user.DomainEvents));
        Assert.Equal(user.UserId, domainEvent.UserId);
        Assert.Equal(roleId, domainEvent.RoleId);
    }

    [Fact]
    public void AssignRole_PreventsDuplicateRole()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        Assert.Throws<RoleAlreadyAssignedDomainException>(() => user.AssignRole(roleId));
        Assert.Single(user.UserRoles);
    }

    [Fact]
    public void AssignRole_RejectsEmptyRoleId()
    {
        var user = CreateUser();

        Assert.Throws<IdentityDomainException>(() => user.AssignRole(Guid.Empty));
        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public void RegisterLogin_UpdatesLastLoginAtUtc()
    {
        var user = CreateUser();
        var loginAtUtc = new DateTime(2026, 9, 9, 8, 30, 0, DateTimeKind.Utc);

        user.RegisterLogin(loginAtUtc);

        Assert.Equal(loginAtUtc, user.LastLoginAtUtc);
    }

    [Fact]
    public void RegisterLogin_RejectsInactiveUser()
    {
        var user = CreateUser();
        user.Deactivate();

        Assert.Throws<UserAuthenticationNotAllowedDomainException>(
            () => user.RegisterLogin(DateTime.UtcNow));
        Assert.Null(user.LastLoginAtUtc);
    }

    [Fact]
    public void RegisterLogin_RejectsSuspendedUser()
    {
        var user = CreateUser();
        user.Suspend();

        Assert.Throws<UserAuthenticationNotAllowedDomainException>(
            () => user.RegisterLogin(DateTime.UtcNow));
        Assert.Null(user.LastLoginAtUtc);
    }

    private static User CreateUser() => User.Create(
        Guid.NewGuid(), "admin", "ADMIN", "admin@smartschool.com", "ADMIN@SMARTSCHOOL.COM",
        "hashed-password", "Ahmed", "Mohamed", "0500000000");
}
