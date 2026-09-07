using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class EmailTests
{
    [Fact]
    public void Create_NormalizesCaseAndWhitespace()
    {
        var result = Email.Create("  User.Name@School.Test  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("user.name@school.test", result.Value.Value);
    }

    [Fact]
    public void EqualEmails_HaveValueEquality()
    {
        var first = Email.Create("user@school.test").Value;
        var second = Email.Create("USER@SCHOOL.TEST").Value;

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}
