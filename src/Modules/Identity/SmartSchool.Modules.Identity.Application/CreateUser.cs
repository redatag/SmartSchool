using System.Net.Mail;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Application;

public sealed record CreateUserCommand(
    Guid SchoolId,
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber);

public sealed record CreateUserResponse(Guid UserId);

public sealed record ValidationError(string Field, string Message);

public sealed class CreateUserResult
{
    private CreateUserResult(Guid? userId, string? errorCode, IReadOnlyCollection<ValidationError> validationErrors)
    {
        UserId = userId;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess => UserId.HasValue;
    public Guid? UserId { get; }
    public string? ErrorCode { get; }
    public IReadOnlyCollection<ValidationError> ValidationErrors { get; }

    public static CreateUserResult Success(Guid userId) => new(userId, null, []);
    public static CreateUserResult Conflict(string errorCode) => new(null, errorCode, []);
    public static CreateUserResult Invalid(IReadOnlyCollection<ValidationError> errors) => new(null, "validation_error", errors);
}

public interface ICreateUserCommandValidator
{
    IReadOnlyCollection<ValidationError> Validate(CreateUserCommand command);
}

public sealed class CreateUserCommandValidator : ICreateUserCommandValidator
{
    public IReadOnlyCollection<ValidationError> Validate(CreateUserCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.SchoolId == Guid.Empty) errors.Add(new(nameof(command.SchoolId), "SchoolId must not be empty."));
        ValidateRequiredLength(command.Username, nameof(command.Username), 3, 100, errors);
        ValidateRequiredLength(command.FirstName, nameof(command.FirstName), 1, 100, errors);
        ValidateRequiredLength(command.LastName, nameof(command.LastName), 1, 100, errors);

        if (string.IsNullOrWhiteSpace(command.Email))
            errors.Add(new(nameof(command.Email), "Email is required."));
        else if (command.Email.Trim().Length > 256 || !MailAddress.TryCreate(command.Email.Trim(), out _))
            errors.Add(new(nameof(command.Email), "Email must be valid."));

        if (string.IsNullOrWhiteSpace(command.Password))
            errors.Add(new(nameof(command.Password), "Password is required."));
        else if (command.Password.Length < 8)
            errors.Add(new(nameof(command.Password), "Password must contain at least 8 characters."));

        if (command.PhoneNumber?.Trim().Length > 30)
            errors.Add(new(nameof(command.PhoneNumber), "PhoneNumber cannot exceed 30 characters."));
        return errors;
    }

    private static void ValidateRequiredLength(string value, string field, int minimum, int maximum, ICollection<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(new(field, $"{field} is required."));
        else if (value.Trim().Length is var length && (length < minimum || length > maximum))
            errors.Add(new(field, $"{field} must contain between {minimum} and {maximum} characters."));
    }
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> UsernameExistsAsync(Guid schoolId, string normalizedUsername, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(Guid schoolId, string normalizedEmail, CancellationToken cancellationToken);
    void Add(User user);
}

public interface IPasswordHasher
{
    string Hash(string password);
}

public interface IIdentityUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class CreateUserCommandHandler(
    ICreateUserCommandValidator validator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<CreateUserResult> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);
        if (errors.Count > 0) return CreateUserResult.Invalid(errors);

        var normalizedUsername = command.Username.Trim().ToUpperInvariant();
        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        if (await users.UsernameExistsAsync(command.SchoolId, normalizedUsername, cancellationToken))
            return CreateUserResult.Conflict("identity.username_exists");
        if (await users.EmailExistsAsync(command.SchoolId, normalizedEmail, cancellationToken))
            return CreateUserResult.Conflict("identity.email_exists");

        var user = User.Create(
            command.SchoolId,
            command.Username.Trim(),
            normalizedUsername,
            command.Email.Trim(),
            normalizedEmail,
            passwordHasher.Hash(command.Password),
            command.FirstName.Trim(),
            command.LastName.Trim(),
            command.PhoneNumber);

        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CreateUserResult.Success(user.UserId);
    }
}
