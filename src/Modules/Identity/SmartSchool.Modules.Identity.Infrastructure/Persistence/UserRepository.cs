using Microsoft.EntityFrameworkCore;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users
            .Include(x => x.UserRoles)
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task<User?> GetByUsernameOrEmailAsync(
        Guid schoolId,
        string normalizedUsernameOrEmail,
        CancellationToken cancellationToken) =>
        dbContext.Users
            .Include(x => x.UserRoles)
            .SingleOrDefaultAsync(
                x => x.SchoolId == schoolId &&
                     (x.NormalizedUsername == normalizedUsernameOrEmail ||
                      x.NormalizedEmail == normalizedUsernameOrEmail),
                cancellationToken);

    public Task<bool> UsernameExistsAsync(Guid schoolId, string normalizedUsername, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(
            x => x.SchoolId == schoolId && x.NormalizedUsername == normalizedUsername,
            cancellationToken);

    public Task<bool> EmailExistsAsync(Guid schoolId, string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(
            x => x.SchoolId == schoolId && x.NormalizedEmail == normalizedEmail,
            cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}
