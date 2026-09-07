using Microsoft.EntityFrameworkCore;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(x => x.Email.Value == normalizedEmail, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users.Include(x => x.RefreshTokens)
            .SingleOrDefaultAsync(x => x.Email.Value == normalizedEmail, cancellationToken);

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.Include(x => x.Roles).SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.Users.Include(x => x.RefreshTokens)
            .SingleOrDefaultAsync(x => x.RefreshTokens.Any(t => t.TokenHash == tokenHash), cancellationToken);

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken) =>
        await (from userRole in dbContext.UserRoles
               join permission in dbContext.RolePermissions on userRole.RoleId equals permission.RoleId
               where userRole.UserId == userId
               select permission.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToArrayAsync(cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}

public sealed class RoleRepository(IdentityDbContext dbContext) : IRoleRepository
{
    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Roles.AnyAsync(x => x.Name == name, cancellationToken);

    public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken) =>
        dbContext.Roles.Include(x => x.Permissions).SingleOrDefaultAsync(x => x.Id == roleId, cancellationToken);

    public void Add(Role role) => dbContext.Roles.Add(role);
}
