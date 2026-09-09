using Microsoft.EntityFrameworkCore;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Persistence;

public sealed class UserAccessReader(IdentityDbContext dbContext) : IUserAccessReader
{
    public async Task<UserAccess> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = await (
                from userRole in dbContext.Set<UserRole>()
                join role in dbContext.Roles on userRole.RoleId equals role.RoleId
                where userRole.UserId == userId
                orderby role.NormalizedName
                select role.Name)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var permissions = await (
                from userRole in dbContext.Set<UserRole>()
                join rolePermission in dbContext.RolePermissions on userRole.RoleId equals rolePermission.RoleId
                join permission in dbContext.Permissions on rolePermission.PermissionId equals permission.PermissionId
                where userRole.UserId == userId
                orderby permission.Code
                select permission.Code)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        return new UserAccess(roles, permissions);
    }
}
