using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Application;

public sealed record RefreshTokenCommand(string RefreshToken);

public enum RefreshTokenStatus
{
    Success,
    Invalid,
    Unauthorized
}

public sealed class RefreshTokenResult
{
    private RefreshTokenResult(
        RefreshTokenStatus status,
        AuthenticationTokens? tokens,
        string? errorCode,
        IReadOnlyCollection<ValidationError> validationErrors)
    {
        Status = status;
        Tokens = tokens;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    public RefreshTokenStatus Status { get; }
    public AuthenticationTokens? Tokens { get; }
    public string? ErrorCode { get; }
    public IReadOnlyCollection<ValidationError> ValidationErrors { get; }

    public static RefreshTokenResult Success(AuthenticationTokens tokens) =>
        new(RefreshTokenStatus.Success, tokens, null, []);

    public static RefreshTokenResult Invalid(IReadOnlyCollection<ValidationError> errors) =>
        new(RefreshTokenStatus.Invalid, null, "validation_error", errors);

    public static RefreshTokenResult Unauthorized() =>
        new(RefreshTokenStatus.Unauthorized, null, "identity.invalid_refresh_token", []);
}

public sealed record AuthenticationTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record GeneratedRefreshToken(
    string Token,
    string TokenHash,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc);

public interface IRefreshTokenCommandValidator
{
    IReadOnlyCollection<ValidationError> Validate(RefreshTokenCommand command);
}

public sealed class RefreshTokenCommandValidator : IRefreshTokenCommandValidator
{
    public IReadOnlyCollection<ValidationError> Validate(RefreshTokenCommand command) =>
        string.IsNullOrWhiteSpace(command.RefreshToken)
            ? [new(nameof(command.RefreshToken), "RefreshToken is required.")]
            : [];
}

public interface IRefreshTokenProvider
{
    GeneratedRefreshToken Generate();
    string Hash(string token);
}

public interface IRefreshTokenRepository
{
    Task<Domain.RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
    void Add(Domain.RefreshToken refreshToken);
}

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenCommandValidator validator,
    IRefreshTokenProvider refreshTokenProvider,
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    IUserAccessReader accessReader,
    ITokenProvider tokenProvider,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<RefreshTokenResult> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);
        if (errors.Count > 0) return RefreshTokenResult.Invalid(errors);

        var tokenHash = refreshTokenProvider.Hash(command.RefreshToken);
        var currentToken = await refreshTokens.GetByHashAsync(tokenHash, cancellationToken);
        var utcNow = DateTime.UtcNow;
        if (currentToken is null || !currentToken.IsActive(utcNow))
            return RefreshTokenResult.Unauthorized();

        var user = await users.GetByIdAsync(currentToken.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
            return RefreshTokenResult.Unauthorized();

        var access = await accessReader.GetAsync(user.UserId, cancellationToken);
        var accessToken = tokenProvider.GenerateAccessToken(user, access.Roles, access.Permissions);
        var generatedRefreshToken = refreshTokenProvider.Generate();
        var replacement = Domain.RefreshToken.Create(
            user.UserId,
            generatedRefreshToken.TokenHash,
            generatedRefreshToken.ExpiresAtUtc,
            generatedRefreshToken.CreatedAtUtc);

        refreshTokens.Add(replacement);
        currentToken.ReplaceWith(replacement.RefreshTokenId, utcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RefreshTokenResult.Success(new AuthenticationTokens(
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            generatedRefreshToken.Token,
            generatedRefreshToken.ExpiresAtUtc));
    }
}
