using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;
using SmartSchool.Modules.Identity.Presentation.Controllers;

namespace SmartSchool.ArchitectureTests;

public sealed class IdentityArchitectureTests
{
    [Fact]
    public void Domain_HasNoInfrastructureReferences()
    {
        var references = typeof(User).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.DoesNotContain(references, x => x is not null &&
            (x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
             x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) ||
             x.Contains("SqlServer", StringComparison.Ordinal) ||
             x.Contains("Redis", StringComparison.Ordinal) ||
             x.Contains("IdentityModel", StringComparison.Ordinal)));
    }

    [Fact]
    public void Application_DoesNotReferenceOuterLayers()
    {
        var references = typeof(CreateUserHandler).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.DoesNotContain("SmartSchool.Modules.Identity.Infrastructure", references);
        Assert.DoesNotContain("SmartSchool.Modules.Identity.Presentation", references);
    }

    [Fact]
    public void Infrastructure_ReferencesApplicationAndDomain()
    {
        var references = typeof(IdentityDbContext).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.Contains("SmartSchool.Modules.Identity.Application", references);
        Assert.Contains("SmartSchool.Modules.Identity.Domain", references);
        Assert.DoesNotContain("SmartSchool.Modules.Identity.Presentation", references);
    }

    [Fact]
    public void Presentation_DoesNotReferenceInfrastructure()
    {
        var references = typeof(IdentityController).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();

        Assert.Contains("SmartSchool.Modules.Identity.Application", references);
        Assert.DoesNotContain("SmartSchool.Modules.Identity.Infrastructure", references);
    }
}
