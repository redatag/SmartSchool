using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class RoleTests
{
    [Fact]
    public void Create_CreatesCustomRoleSuccessfully()
    {
        var schoolId = Guid.NewGuid();

        var role = Role.Create(schoolId, " Teacher ", " School teacher role ");

        Assert.Equal(schoolId, role.SchoolId);
        Assert.Equal("Teacher", role.Name);
        Assert.Equal("School teacher role", role.Description);
        Assert.False(role.IsSystemRole);
    }

    [Fact]
    public void Create_AllowsExplicitSystemRole()
    {
        var role = Role.Create(Guid.NewGuid(), "Administrator", isSystemRole: true);

        Assert.True(role.IsSystemRole);
    }

    [Fact]
    public void Create_CreatesVersion7Id()
    {
        var role = Role.Create(Guid.NewGuid(), "Teacher");

        Assert.NotEqual(Guid.Empty, role.RoleId);
        Assert.Equal(7, role.RoleId.Version);
    }

    [Fact]
    public void Create_NormalizesName()
    {
        var role = Role.Create(Guid.NewGuid(), " School Teacher ");

        Assert.Equal("SCHOOL TEACHER", role.NormalizedName);
    }

    [Fact]
    public void Create_RaisesRoleCreatedDomainEvent()
    {
        var role = Role.Create(Guid.NewGuid(), "Teacher");

        var domainEvent = Assert.IsType<RoleCreatedDomainEvent>(Assert.Single(role.DomainEvents));
        Assert.Equal(role.RoleId, domainEvent.RoleId);
        Assert.Equal(role.SchoolId, domainEvent.SchoolId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsInvalidName(string name)
    {
        Assert.Throws<IdentityDomainException>(() => Role.Create(Guid.NewGuid(), name));
    }

    [Fact]
    public void Create_RejectsEmptySchoolId()
    {
        Assert.Throws<IdentityDomainException>(() => Role.Create(Guid.Empty, "Teacher"));
    }

    [Fact]
    public void GrantPermission_AddsPermissionOnce()
    {
        var role = Role.Create(Guid.NewGuid(), "Administrator");
        var permissionId = Guid.NewGuid();

        role.GrantPermission(permissionId);
        role.GrantPermission(permissionId);

        var rolePermission = Assert.Single(role.RolePermissions);
        Assert.Equal(role.RoleId, rolePermission.RoleId);
        Assert.Equal(permissionId, rolePermission.PermissionId);
    }
}
