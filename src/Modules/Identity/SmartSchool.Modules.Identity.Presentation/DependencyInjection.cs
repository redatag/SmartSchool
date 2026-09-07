using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Presentation.Authorization;

namespace SmartSchool.Modules.Identity.Presentation;

public static class DependencyInjection
{
    public static IMvcBuilder AddIdentityPresentation(this IServiceCollection services)
    {
        services.AddScoped<CreateUserHandler>();
        services.AddScoped<CreateRoleHandler>();
        services.AddScoped<GrantPermissionHandler>();
        services.AddScoped<AssignRoleHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        return services.AddControllers().AddApplicationPart(typeof(DependencyInjection).Assembly);
    }
}
