using SmartSchool.Modules.Identity.Domain;
using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Application;

public sealed record LoginCommand(string Email, string Password);
public sealed record RefreshTokenCommand(string RefreshToken);
public sealed record AuthenticationResponse(string AccessToken, string RefreshToken, DateTimeOffset RefreshTokenExpiresAtUtc);

public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokens,
    IClock clock,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<Result<AuthenticationResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = Email.Create(command.Email);
        if (email.IsFailure) return Result.Failure<AuthenticationResponse>(IdentityApplicationErrors.InvalidCredentials);

        var user = await users.GetByEmailAsync(email.Value.Value, cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.Verify(command.Password, user.PasswordHash))
            return Result.Failure<AuthenticationResponse>(IdentityApplicationErrors.InvalidCredentials);

        var permissions = await users.GetPermissionsAsync(user.Id, cancellationToken);
        var rawRefreshToken = tokens.CreateRefreshToken();
        var refreshExpiry = clock.UtcNow.AddDays(30);
        user.IssueRefreshToken(tokens.HashRefreshToken(rawRefreshToken), refreshExpiry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthenticationResponse(
            tokens.CreateAccessToken(user.Id, user.Email.Value, permissions), rawRefreshToken, refreshExpiry));
    }
}

public sealed class RefreshTokenHandler(
    IUserRepository users,
    ITokenService tokens,
    IClock clock,
    IIdentityUnitOfWork unitOfWork)
{
    public async Task<Result<AuthenticationResponse>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var currentHash = tokens.HashRefreshToken(command.RefreshToken);
        var user = await users.GetByRefreshTokenHashAsync(currentHash, cancellationToken);
        if (user is null) return Result.Failure<AuthenticationResponse>(IdentityErrors.InvalidRefreshToken);

        var replacement = tokens.CreateRefreshToken();
        var replacementHash = tokens.HashRefreshToken(replacement);
        var expiry = clock.UtcNow.AddDays(30);
        var rotation = user.RotateRefreshToken(currentHash, replacementHash, expiry, clock.UtcNow);
        if (rotation.IsFailure) return Result.Failure<AuthenticationResponse>(rotation.Error);

        var permissions = await users.GetPermissionsAsync(user.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(new AuthenticationResponse(
            tokens.CreateAccessToken(user.Id, user.Email.Value, permissions), replacement, expiry));
    }
}
