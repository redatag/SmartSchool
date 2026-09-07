using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class RoleTests
{
    [Fact]
    public void GrantPermission_AcceptsStablePermissionCode()
    {
        var role = Role.Create("Teacher").Value;

        var result = role.GrantPermission(Permissions.AttendanceRecord);

        Assert.True(result.IsSuccess);
        Assert.Contains(role.Permissions, x => x.Code == Permissions.AttendanceRecord);
        var permission = Assert.Single(role.Permissions);
        Assert.Equal(role.Id, permission.RoleId);
    }

    [Fact]
    public void GrantPermission_RejectsUnknownAndDuplicateCodes()
    {
        var role = Role.Create("Teacher").Value;

        Assert.True(role.GrantPermission("made.up").IsFailure);
        Assert.True(role.GrantPermission(Permissions.StudentsView).IsSuccess);
        Assert.Equal(IdentityErrors.DuplicatePermission, role.GrantPermission(Permissions.StudentsView).Error);
    }


    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public void Create_RejectsInvalidName(string name)
    {
        var result = Role.Create(name);

        Assert.True(result.IsFailure);
        Assert.Equal(IdentityErrors.InvalidRoleName, result.Error);
    }
}
