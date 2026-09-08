using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Application;

public sealed record LoginCommand(Guid SchoolId, string UsernameOrEmail, string Password);

public enum LoginStatus
{
    Success,
    Invalid,
    InvalidCredentials,
    Forbidden
}

public sealed class LoginResult
{
    private LoginResult(
        LoginStatus status,
        string? accessToken,
        DateTime? expiresAtUtc,
        Guid? userId,
        string? username,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        string? errorCode,
        IReadOnlyCollection<ValidationError> validationErrors)
    {
        Status = status;
        AccessToken = accessToken;
        ExpiresAtUtc = expiresAtUtc;
        UserId = userId;
        Username = username;
        Roles = roles;
        Permissions = permissions;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    public LoginStatus Status { get; }
    public string? AccessToken { get; }
    public DateTime? ExpiresAtUtc { get; }
    public Guid? UserId { get; }
    public string? Username { get; }
    public IReadOnlyCollection<string> Roles { get; }
    public IReadOnlyCollection<string> Permissions { get; }
    public string? ErrorCode { get; }
    public IReadOnlyCollection<ValidationError> ValidationErrors { get; }

    public static LoginResult Success(User user, UserAccess access, AccessTokenResult token) =>
        new(
            LoginStatus.Success,
            token.AccessToken,
            token.ExpiresAtUtc,
            user.UserId,
            user.Username,
            access.Roles,
            access.Permissions,
            null,
            []);

    public static LoginResult Invalid(IReadOnlyCollection<ValidationError> errors) =>
        new(LoginStatus.Invalid, null, null, null, null, [], [], "validation_error", errors);

    public static LoginResult InvalidCredentials() =>
        new(LoginStatus.InvalidCredentials, null, null, null, null, [], [], "identity.invalid_credentials", []);

    public static LoginResult Forbidden() =>
        new(LoginStatus.Forbidden, null, null, null, null, [], [], "identity.account_inactive", []);
}

public sealed record UserAccess(
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);

public interface ILoginCommandValidator
{
    IReadOnlyCollection<ValidationError> Validate(LoginCommand command);
}

public sealed class LoginCommandValidator : ILoginCommandValidator
{
    public IReadOnlyCollection<ValidationError> Validate(LoginCommand command)
    {
        var errors = new List<ValidationError>();
        if (command.SchoolId == Guid.Empty)
            errors.Add(new(nameof(command.SchoolId), "SchoolId must not be empty."));
        if (string.IsNullOrWhiteSpace(command.UsernameOrEmail))
            errors.Add(new(nameof(command.UsernameOrEmail), "UsernameOrEmail is required."));
        if (string.IsNullOrWhiteSpace(command.Password))
            errors.Add(new(nameof(command.Password), "Password is required."));
        return errors;
    }
}

public interface IUserAccessReader
{
    Task<UserAccess> GetAsync(Guid userId, CancellationToken cancellationToken);
}

public interface ITokenProvider
{
    AccessTokenResult GenerateAccessToken(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions);
}

public sealed class LoginCommandHandler(
    ILoginCommandValidator validator,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUserAccessReader accessReader,
    ITokenProvider tokenProvider,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);
        if (errors.Count > 0) return LoginResult.Invalid(errors);

        var normalizedUsernameOrEmail = command.UsernameOrEmail.Trim().ToUpperInvariant();
        var user = await users.GetByUsernameOrEmailAsync(
            command.SchoolId,
            normalizedUsernameOrEmail,
            cancellationToken);

        if (user is null) return LoginResult.InvalidCredentials();
        if (user.Status != UserStatus.Active) return LoginResult.Forbidden();
        if (!passwordHasher.Verify(user.PasswordHash, command.Password))
            return LoginResult.InvalidCredentials();

        var access = await accessReader.GetAsync(user.UserId, cancellationToken);
        var loginAtUtc = DateTime.UtcNow;
        user.RegisterLogin(loginAtUtc);
        var token = tokenProvider.GenerateAccessToken(user, access.Roles, access.Permissions);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return LoginResult.Success(user, access, token);
    }
}
