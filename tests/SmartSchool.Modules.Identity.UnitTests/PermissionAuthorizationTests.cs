using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SmartSchool.Modules.Identity.Domain;
using SmartSchool.Modules.Identity.Presentation.Authorization;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public async Task Handler_SucceedsOnlyWhenPermissionClaimMatches()
    {
        var requirement = new PermissionRequirement(Permissions.AttendanceRecord);
        var allowedContext = Context(requirement, Permissions.AttendanceRecord);
        var deniedContext = Context(requirement, Permissions.AttendanceView);
        var handler = new PermissionAuthorizationHandler();

        await handler.HandleAsync(allowedContext);
        await handler.HandleAsync(deniedContext);

        Assert.True(allowedContext.HasSucceeded);
        Assert.False(deniedContext.HasSucceeded);
    }

    private static AuthorizationHandlerContext Context(IAuthorizationRequirement requirement, string permission)
    {
        var identity = new ClaimsIdentity([new Claim("permission", permission)], "test");
        return new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), null);
    }
}
