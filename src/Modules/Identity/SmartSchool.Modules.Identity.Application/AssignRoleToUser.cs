using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Application;

public sealed record AssignRoleToUserCommand(Guid UserId, Guid RoleId);

public enum AssignRoleToUserStatus
{
    Success,
    Invalid,
    NotFound,
    Conflict
}

public sealed class AssignRoleToUserResult
{
    private AssignRoleToUserResult(
        AssignRoleToUserStatus status,
        string? errorCode,
        IReadOnlyCollection<ValidationError> validationErrors)
    {
        Status = status;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    public AssignRoleToUserStatus Status { get; }
    public string? ErrorCode { get; }
    public IReadOnlyCollection<ValidationError> ValidationErrors { get; }

    public static AssignRoleToUserResult Success() => new(AssignRoleToUserStatus.Success, null, []);
    public static AssignRoleToUserResult Invalid(IReadOnlyCollection<ValidationError> errors) =>
        new(AssignRoleToUserStatus.Invalid, "validation_error", errors);
    public static AssignRoleToUserResult NotFound(string errorCode) =>
        new(AssignRoleToUserStatus.NotFound, errorCode, []);
    public static AssignRoleToUserResult Conflict(string errorCode) =>
        new(AssignRoleToUserStatus.Conflict, errorCode, []);
}

public interface IAssignRoleToUserCommandValidator
{
    IReadOnlyCollection<ValidationError> Validate(AssignRoleToUserCommand command);
}

public sealed class AssignRoleToUserCommandValidator : IAssignRoleToUserCommandValidator
{
    public IReadOnlyCollection<ValidationError> Validate(AssignRoleToUserCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.UserId == Guid.Empty)
            errors.Add(new(nameof(command.UserId), "UserId must not be empty."));
        if (command.RoleId == Guid.Empty)
            errors.Add(new(nameof(command.RoleId), "RoleId must not be empty."));
        return errors;
    }
}

public sealed class AssignRoleToUserCommandHandler(
    IAssignRoleToUserCommandValidator validator,
    IUserRepository users,
    IRoleRepository roles,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<AssignRoleToUserResult> HandleAsync(
        AssignRoleToUserCommand command,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);
        if (errors.Count > 0) return AssignRoleToUserResult.Invalid(errors);

        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null) return AssignRoleToUserResult.NotFound("identity.user_not_found");

        var role = await roles.GetByIdAsync(command.RoleId, cancellationToken);
        if (role is null) return AssignRoleToUserResult.NotFound("identity.role_not_found");

        if (user.SchoolId != role.SchoolId)
            return AssignRoleToUserResult.Conflict("identity.role_school_mismatch");

        try
        {
            user.AssignRole(role.RoleId);
        }
        catch (RoleAlreadyAssignedDomainException)
        {
            return AssignRoleToUserResult.Conflict("identity.role_already_assigned");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return AssignRoleToUserResult.Success();
    }
}
