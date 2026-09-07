using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Domain;

public sealed class User : AggregateRoot<Guid>
{
    private readonly List<UserRole> _roles = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    private User(Guid id, string username, Email email, string displayName, string passwordHash) : base(id)
    {
        Username = username;
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        Raise(new UserCreatedDomainEvent(id, username, email.Value, CreatedAtUtc));
    }

    private User() { }

    public string Username { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private init; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static Result<User> Create(string email, string displayName, string passwordHash)
    {
        var emailResult = Email.Create(email);
        if (emailResult.IsFailure) return Result.Failure<User>(emailResult.Error);

        var username = emailResult.Value.Value[..emailResult.Value.Value.IndexOf('@')];
        return Create(username, emailResult.Value.Value, displayName, passwordHash);
    }

    public static Result<User> Create(string username, string email, string displayName, string passwordHash)
    {
        var normalizedUsername = username?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!IsValidUsername(normalizedUsername))
            return Result.Failure<User>(IdentityErrors.InvalidUsername);

        var emailResult = Email.Create(email);
        if (emailResult.IsFailure) return Result.Failure<User>(emailResult.Error);

        var normalizedName = displayName?.Trim() ?? string.Empty;
        if (normalizedName.Length is < 2 or > 200)
            return Result.Failure<User>(IdentityErrors.InvalidName);

        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        return Result.Success(new User(Guid.NewGuid(), normalizedUsername, emailResult.Value, normalizedName, passwordHash));
    }

    public void Activate()
    {
        if (IsActive) return;

        IsActive = true;
        Raise(new UserActivatedDomainEvent(Id, DateTimeOffset.UtcNow));
    }

    public void Deactivate()
    {
        if (!IsActive) return;

        IsActive = false;
        Raise(new UserDeactivatedDomainEvent(Id, DateTimeOffset.UtcNow));
    }

    public Result RegisterLogin(DateTimeOffset occurredOnUtc)
    {
        if (!IsActive) return Result.Failure(IdentityErrors.UserInactive);

        LastLoginAtUtc = occurredOnUtc;
        return Result.Success();
    }

    public Result AssignRole(Guid roleId)
    {
        if (_roles.Any(x => x.RoleId == roleId)) return Result.Failure(IdentityErrors.DuplicateRole);
        _roles.Add(new UserRole(Guid.NewGuid(), Id, roleId));
        Raise(new RoleAssignedDomainEvent(Id, roleId, DateTimeOffset.UtcNow));
        return Result.Success();
    }

    public Result RemoveRole(Guid roleId)
    {
        var assignment = _roles.SingleOrDefault(x => x.RoleId == roleId);
        if (assignment is null) return Result.Failure(IdentityErrors.RoleNotAssigned);

        _roles.Remove(assignment);
        return Result.Success();
    }

    private static bool IsValidUsername(string username) =>
        username.Length is >= 3 and <= 50 &&
        char.IsAsciiLetterOrDigit(username[0]) &&
        username.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');

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
