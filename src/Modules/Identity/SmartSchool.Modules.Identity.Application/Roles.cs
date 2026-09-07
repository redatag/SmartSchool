using SmartSchool.Modules.Identity.Domain;
using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Application;

public sealed record CreateRoleCommand(string Name);
public sealed record GrantPermissionCommand(Guid RoleId, string PermissionCode);
public sealed record AssignRoleCommand(Guid UserId, Guid RoleId);
public sealed record RoleResponse(Guid Id, string Name, IReadOnlyCollection<string> Permissions);

public sealed class CreateRoleHandler(IRoleRepository roles, IIdentityUnitOfWork unitOfWork)
{
    public async Task<Result<RoleResponse>> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var normalizedName = command.Name?.Trim() ?? string.Empty;
        if (await roles.NameExistsAsync(normalizedName, cancellationToken))
            return Result.Failure<RoleResponse>(IdentityApplicationErrors.RoleAlreadyExists);

        var result = Role.Create(normalizedName);
        if (result.IsFailure) return Result.Failure<RoleResponse>(result.Error);

        roles.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new RoleResponse(result.Value.Id, result.Value.Name, []));
    }
}

public sealed class GrantPermissionHandler(IRoleRepository roles, IIdentityUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(GrantPermissionCommand command, CancellationToken cancellationToken)
    {
        var role = await roles.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null) return Result.Failure(IdentityApplicationErrors.RoleNotFound);
        var result = role.GrantPermission(command.PermissionCode);
        if (result.IsFailure) return result;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class AssignRoleHandler(IUserRepository users, IRoleRepository roles, IIdentityUnitOfWork unitOfWork)
{
    public async Task<Result> HandleAsync(AssignRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) return Result.Failure(IdentityApplicationErrors.UserNotFound);
        if (await roles.GetByIdAsync(command.RoleId, cancellationToken) is null)
            return Result.Failure(IdentityApplicationErrors.RoleNotFound);

        var result = user.AssignRole(command.RoleId);
        if (result.IsFailure) return result;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
