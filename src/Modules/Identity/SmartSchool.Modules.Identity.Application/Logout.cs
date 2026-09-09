namespace SmartSchool.Modules.Identity.Application;

public sealed record LogoutCommand(string RefreshToken);

public sealed class LogoutResult
{
    private LogoutResult(bool isSuccess, IReadOnlyCollection<ValidationError> validationErrors)
    {
        IsSuccess = isSuccess;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }
    public IReadOnlyCollection<ValidationError> ValidationErrors { get; }

    public static LogoutResult Success() => new(true, []);
    public static LogoutResult Invalid(IReadOnlyCollection<ValidationError> errors) => new(false, errors);
}

public interface ILogoutCommandValidator
{
    IReadOnlyCollection<ValidationError> Validate(LogoutCommand command);
}

public sealed class LogoutCommandValidator : ILogoutCommandValidator
{
    public IReadOnlyCollection<ValidationError> Validate(LogoutCommand command) =>
        string.IsNullOrWhiteSpace(command.RefreshToken)
            ? [new(nameof(command.RefreshToken), "RefreshToken is required.")]
            : [];
}

public sealed class LogoutCommandHandler(
    ILogoutCommandValidator validator,
    IRefreshTokenProvider refreshTokenProvider,
    IRefreshTokenRepository refreshTokens,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<LogoutResult> HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);
        if (errors.Count > 0) return LogoutResult.Invalid(errors);

        var tokenHash = refreshTokenProvider.Hash(command.RefreshToken);
        var refreshToken = await refreshTokens.GetByHashAsync(tokenHash, cancellationToken);
        if (refreshToken is null || refreshToken.IsRevoked)
            return LogoutResult.Success();

        refreshToken.Revoke(DateTime.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return LogoutResult.Success();
    }
}
