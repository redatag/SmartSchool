using Microsoft.EntityFrameworkCore;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

public sealed class RoleRepository(IdentityDbContext dbContext) : IRoleRepository
{
    public Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken) =>
        dbContext.Roles.SingleOrDefaultAsync(x => x.RoleId == roleId, cancellationToken);

    public Task<bool> NameExistsAsync(Guid schoolId, string normalizedName, CancellationToken cancellationToken) =>
        dbContext.Roles.AnyAsync(
            x => x.SchoolId == schoolId && x.NormalizedName == normalizedName,
            cancellationToken);

    public void Add(Role role) => dbContext.Roles.Add(role);
}
