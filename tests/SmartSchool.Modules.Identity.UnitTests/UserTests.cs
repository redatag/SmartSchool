using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class UserTests
{
    [Fact]
    public void Create_NormalizesEmailAndRaisesDomainEvent()
    {
        var result = User.Create("  Principal@School.test ", "School Principal", "stored-hash");

        Assert.True(result.IsSuccess);
        Assert.Equal("principal@school.test", result.Value.Email.Value);
        Assert.Single(result.Value.DomainEvents, x => x is UserCreatedDomainEvent);
    }

    [Fact]
    public void AssignRole_RejectsDuplicateRole()
    {
        var user = User.Create("user@school.test", "Test User", "stored-hash").Value;
        var roleId = Guid.NewGuid();

        Assert.True(user.AssignRole(roleId).IsSuccess);
        var duplicate = user.AssignRole(roleId);

        Assert.True(duplicate.IsFailure);
        Assert.Equal(IdentityErrors.DuplicateRole, duplicate.Error);
    }

    [Fact]
    public void RotateRefreshToken_RevokesOldTokenAndAddsReplacement()
    {
        var user = User.Create("user@school.test", "Test User", "stored-hash").Value;
        user.IssueRefreshToken("old-hash", DateTimeOffset.UtcNow.AddHours(1));

        var now = DateTimeOffset.UtcNow;
        var result = user.RotateRefreshToken("old-hash", "new-hash", now.AddDays(1), now);

        Assert.True(result.IsSuccess);
        Assert.False(user.RefreshTokens.Single(x => x.TokenHash == "old-hash").IsActive(DateTimeOffset.UtcNow));
        Assert.Contains(user.RefreshTokens, x => x.TokenHash == "new-hash");
        Assert.True(user.RotateRefreshToken("old-hash", "another-hash", now.AddDays(1), now).IsFailure);
    }
}
