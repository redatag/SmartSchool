using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;
using SmartSchool.Modules.Identity.Infrastructure.Security;

namespace SmartSchool.Modules.Identity.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is required.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IIdentityUnitOfWork>(provider => provider.GetRequiredService<IdentityDbContext>());
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ICreateUserCommandValidator, CreateUserCommandValidator>();
        services.AddSingleton<ICreateRoleCommandValidator, CreateRoleCommandValidator>();
        services.AddScoped<CreateUserCommandHandler>();
        services.AddScoped<CreateRoleCommandHandler>();
        return services;
    }
}
