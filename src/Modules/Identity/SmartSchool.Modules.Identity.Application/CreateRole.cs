using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Application;

public sealed record CreateRoleCommand(Guid SchoolId, string Name, string? Description);

public sealed record CreateRoleResponse(Guid RoleId);

public sealed class CreateRoleResult
{
    private CreateRoleResult(Guid? roleId, string? errorCode, IReadOnlyCollection<ValidationError> validationErrors)
    {
        RoleId = roleId;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess => RoleId.HasValue;
    public Guid? RoleId { get; }
    public string? ErrorCode { get; }
    public IReadOnlyCollection<ValidationError> ValidationErrors { get; }

    public static CreateRoleResult Success(Guid roleId) => new(roleId, null, []);
    public static CreateRoleResult Conflict(string errorCode) => new(null, errorCode, []);
    public static CreateRoleResult Invalid(IReadOnlyCollection<ValidationError> errors) =>
        new(null, "validation_error", errors);
}

public interface ICreateRoleCommandValidator
{
    IReadOnlyCollection<ValidationError> Validate(CreateRoleCommand command);
}

public sealed class CreateRoleCommandValidator : ICreateRoleCommandValidator
{
    public IReadOnlyCollection<ValidationError> Validate(CreateRoleCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.SchoolId == Guid.Empty)
            errors.Add(new(nameof(command.SchoolId), "SchoolId must not be empty."));

        if (string.IsNullOrWhiteSpace(command.Name))
            errors.Add(new(nameof(command.Name), "Name is required."));
        else if (command.Name.Trim().Length > 100)
            errors.Add(new(nameof(command.Name), "Name cannot exceed 100 characters."));

        if (command.Description?.Trim().Length > 500)
            errors.Add(new(nameof(command.Description), "Description cannot exceed 500 characters."));

        return errors;
    }
}

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(Guid schoolId, string normalizedName, CancellationToken cancellationToken);
    void Add(Role role);
}

public sealed class CreateRoleCommandHandler(
    ICreateRoleCommandValidator validator,
    IRoleRepository roles,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<CreateRoleResult> HandleAsync(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);
        if (errors.Count > 0) return CreateRoleResult.Invalid(errors);

        var normalizedName = Role.NormalizeName(command.Name);
        if (await roles.NameExistsAsync(command.SchoolId, normalizedName, cancellationToken))
            return CreateRoleResult.Conflict("identity.role_name_exists");

        var role = Role.Create(command.SchoolId, command.Name, command.Description);
        roles.Add(role);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateRoleResult.Success(role.RoleId);
    }
}
