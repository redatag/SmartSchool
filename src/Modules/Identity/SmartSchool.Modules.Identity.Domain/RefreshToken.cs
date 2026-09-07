using SmartSchool.SharedKernel;

namespace SmartSchool.Modules.Identity.Domain;

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
