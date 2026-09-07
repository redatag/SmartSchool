using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Domain;

public sealed class User : AggregateRoot<Guid>
{
    private readonly List<UserRole> _roles = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User(Guid id, Email email, string displayName, string passwordHash) : base(id)
    {
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Raise(new UserCreatedDomainEvent(id, email.Value, CreatedAtUtc));
    }

    private User() { }

    public Email Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private init; }
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static Result<User> Create(string email, string displayName, string passwordHash)
    {
        var emailResult = Email.Create(email);
        if (emailResult.IsFailure) return Result.Failure<User>(emailResult.Error);

        var normalizedName = displayName?.Trim() ?? string.Empty;
        if (normalizedName.Length is < 2 or > 200)
            return Result.Failure<User>(IdentityErrors.InvalidName);

        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        return Result.Success(new User(Guid.NewGuid(), emailResult.Value, normalizedName, passwordHash));
    }

    public Result AssignRole(Guid roleId)
    {
        if (_roles.Any(x => x.RoleId == roleId)) return Result.Failure(IdentityErrors.DuplicateRole);
        _roles.Add(new UserRole(Guid.NewGuid(), Id, roleId));
        Raise(new RoleAssignedToUserDomainEvent(Id, roleId, DateTimeOffset.UtcNow));
        return Result.Success();
    }

    public void IssueRefreshToken(string tokenHash, DateTimeOffset expiresAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        _refreshTokens.Add(new RefreshToken(Guid.NewGuid(), Id, tokenHash, expiresAtUtc));
    }

    public Result RotateRefreshToken(string currentHash, string replacementHash, DateTimeOffset replacementExpiresAtUtc, DateTimeOffset nowUtc)
    {
        var token = _refreshTokens.SingleOrDefault(x => x.TokenHash == currentHash);
        if (token is null || !token.IsActive(nowUtc))
            return Result.Failure(IdentityErrors.InvalidRefreshToken);

        token.Revoke(replacementHash, nowUtc);
        IssueRefreshToken(replacementHash, replacementExpiresAtUtc);
        return Result.Success();
    }
}

public sealed class UserRole : Entity<Guid>
{
    internal UserRole(Guid id, Guid userId, Guid roleId) : base(id)
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = DateTimeOffset.UtcNow;
    }

    private UserRole() { }
    public Guid UserId { get; private init; }
    public Guid RoleId { get; private init; }
    public DateTimeOffset AssignedAtUtc { get; private init; }
}

public sealed class RefreshToken : Entity<Guid>
{
    internal RefreshToken(Guid id, Guid userId, string tokenHash, DateTimeOffset expiresAtUtc) : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private RefreshToken() { }
    public Guid UserId { get; private init; }
    public string TokenHash { get; private init; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private init; }
    public DateTimeOffset CreatedAtUtc { get; private init; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public bool IsActive(DateTimeOffset nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    internal void Revoke(string replacementHash, DateTimeOffset nowUtc)
    {
        RevokedAtUtc = nowUtc;
        ReplacedByTokenHash = replacementHash;
    }
}

public sealed record UserCreatedDomainEvent(Guid UserId, string Email, DateTimeOffset OccurredOnUtc) : IDomainEvent;
public sealed record RoleAssignedToUserDomainEvent(Guid UserId, Guid RoleId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
