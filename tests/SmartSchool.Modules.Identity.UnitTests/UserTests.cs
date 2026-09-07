using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class UserTests
{
    [Fact]
    public void Create_NormalizesEmailAndRaisesDomainEvent()
    {
        var result = User.Create(" Principal.User ", "  Principal@School.test ", "School Principal", "stored-hash");

        Assert.True(result.IsSuccess);
        Assert.Equal("principal.user", result.Value.Username);
        Assert.Equal("principal@school.test", result.Value.Email.Value);
        var domainEvent = Assert.IsType<UserCreatedDomainEvent>(Assert.Single(result.Value.DomainEvents));
        Assert.Equal(result.Value.Id, domainEvent.UserId);
        Assert.Equal("principal.user", domainEvent.Username);
        Assert.Equal("principal@school.test", domainEvent.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("invalid username")]
    [InlineData("invalid+")]
    [InlineData("_cannot-start-with-symbol")]
    public void Create_RejectsInvalidUsername(string username)
    {
        var result = User.Create(username, "user@school.test", "Test User", "stored-hash");

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidUsername, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-domain@")]
    public void Create_RejectsInvalidEmail(string email)
    {
        var result = User.Create("test.user", email, "Test User", "stored-hash");

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidEmail, result.Error);
    }

    [Fact]
    public void AssignRole_RejectsDuplicateRole()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();

        Assert.True(user.AssignRole(roleId).IsSuccess);
        var duplicate = user.AssignRole(roleId);

        Assert.True(duplicate.IsFailure);
        Assert.Equal(IdentityErrors.DuplicateRole, duplicate.Error);
        Assert.Single(user.Roles);
        var domainEvent = Assert.IsType<RoleAssignedDomainEvent>(user.DomainEvents.Last());
        Assert.Equal(user.Id, domainEvent.UserId);
        Assert.Equal(roleId, domainEvent.RoleId);
    }

    [Fact]
    public void RemoveRole_RemovesExistingAssignment()
    {
        var user = CreateUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        var result = user.RemoveRole(roleId);

        Assert.True(result.IsSuccess);
        Assert.Empty(user.Roles);
    }

    [Fact]
    public void RemoveRole_RejectsRoleThatIsNotAssigned()
    {
        var user = CreateUser();

        var result = user.RemoveRole(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.RoleNotAssigned, result.Error);
    }

    [Fact]
    public void DeactivateAndActivate_ChangeStateAndRaiseEventsOnce()
    {
        var user = CreateUser();
        user.ClearDomainEvents();

        user.Deactivate();
        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.Single(user.DomainEvents);
        Assert.IsType<UserDeactivatedDomainEvent>(user.DomainEvents.Single());

        user.Activate();
        user.Activate();

        Assert.True(user.IsActive);
        Assert.Equal(2, user.DomainEvents.Count);
        Assert.IsType<UserActivatedDomainEvent>(user.DomainEvents.Last());
    }

    [Fact]
    public void RegisterLogin_RecordsLoginForActiveUser()
    {
        var user = CreateUser();
        var occurredOnUtc = new DateTimeOffset(2026, 9, 8, 1, 2, 3, TimeSpan.Zero);

        var result = user.RegisterLogin(occurredOnUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(occurredOnUtc, user.LastLoginAtUtc);
    }

    [Fact]
    public void RegisterLogin_RejectsDeactivatedUser()
    {
        var user = CreateUser();
        user.Deactivate();

        var result = user.RegisterLogin(DateTimeOffset.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.UserInactive, result.Error);
        Assert.Null(user.LastLoginAtUtc);
    }

    [Fact]
    public void RotateRefreshToken_RevokesOldTokenAndAddsReplacement()
    {
        var user = CreateUser();
        user.IssueRefreshToken("old-hash", DateTimeOffset.UtcNow.AddHours(1));

        var now = DateTimeOffset.UtcNow;
        var result = user.RotateRefreshToken("old-hash", "new-hash", now.AddDays(1), now);

        Assert.True(result.IsSuccess);
        Assert.False(user.RefreshTokens.Single(x => x.TokenHash == "old-hash").IsActive(DateTimeOffset.UtcNow));
        Assert.Contains(user.RefreshTokens, x => x.TokenHash == "new-hash");
        Assert.True(user.RotateRefreshToken("old-hash", "another-hash", now.AddDays(1), now).IsFailure);
    }

    private static User CreateUser() =>
        User.Create("test.user", "user@school.test", "Test User", "stored-hash").Value;
}
