using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;
using SmartSchool.Modules.Identity.Infrastructure.Security;
using SmartSchool.Modules.Identity.Presentation;

namespace SmartSchool.ArchitectureTests;

public sealed class IdentityArchitectureTests
{
    [Fact]
    public void Domain_HasNoInfrastructureOrAspNetReferences()
    {
        var references = ReferencesOf(typeof(User));

        Assert.DoesNotContain("SmartSchool.Modules.Identity.Infrastructure", references);
        Assert.DoesNotContain("SmartSchool.Modules.Identity.Presentation", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.Contains("IdentityModel", StringComparison.Ordinal));
    }

    [Fact]
    public void Application_HasNoInfrastructureReference()
    {
        Assert.DoesNotContain(
            "SmartSchool.Modules.Identity.Infrastructure",
            ReferencesOf(typeof(CreateUserCommandHandler)));
    }

    [Fact]
    public void IdentityProjects_DoNotReferenceOtherModuleInfrastructure()
    {
        var assemblies = new[]
        {
            typeof(User).Assembly,
            typeof(CreateUserCommandHandler).Assembly,
            typeof(UserRepository).Assembly,
            typeof(UsersController).Assembly
        };

        foreach (var assembly in assemblies)
        {
            Assert.DoesNotContain(
                assembly.GetReferencedAssemblies(),
                reference => reference.Name is { } name &&
                    name.StartsWith("SmartSchool.Modules.", StringComparison.Ordinal) &&
                    name.EndsWith(".Infrastructure", StringComparison.Ordinal) &&
                    name != "SmartSchool.Modules.Identity.Infrastructure");
        }
    }

    [Fact]
    public void Presentation_ReferencesApplicationButNotInfrastructure()
    {
        var references = ReferencesOf(typeof(UsersController));

        Assert.Contains("SmartSchool.Modules.Identity.Application", references);
        Assert.DoesNotContain("SmartSchool.Modules.Identity.Infrastructure", references);
    }

    [Fact]
    public void Infrastructure_ImplementsApplicationAbstractions()
    {
        Assert.Contains(typeof(IUserRepository), typeof(UserRepository).GetInterfaces());
        Assert.Contains(typeof(IRoleRepository), typeof(RoleRepository).GetInterfaces());
        Assert.Contains(typeof(ITokenProvider), typeof(JwtTokenProvider).GetInterfaces());
        Assert.Contains(typeof(IRefreshTokenProvider), typeof(RefreshTokenProvider).GetInterfaces());
        Assert.Contains(typeof(IRefreshTokenRepository), typeof(RefreshTokenRepository).GetInterfaces());
        Assert.Contains(typeof(IIdentityUnitOfWork), typeof(IdentityDbContext).GetInterfaces());
    }

    private static string[] ReferencesOf(Type marker) =>
        marker.Assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToArray();
}
