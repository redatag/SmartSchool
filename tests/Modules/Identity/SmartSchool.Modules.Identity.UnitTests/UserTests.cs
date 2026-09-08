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

    private static User CreateUser() => User.Create(
        Guid.NewGuid(), "admin", "ADMIN", "admin@smartschool.com", "ADMIN@SMARTSCHOOL.COM",
        "hashed-password", "Ahmed", "Mohamed", "0500000000");
}
