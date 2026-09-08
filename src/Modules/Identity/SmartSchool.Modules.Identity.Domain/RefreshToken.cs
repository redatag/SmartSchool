namespace SmartSchool.Modules.Identity.Domain;

public sealed class RefreshToken
{
    private RefreshToken(
        Guid refreshTokenId,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc,
        string? createdByIp)
    {
        RefreshTokenId = refreshTokenId;
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
        CreatedByIp = createdByIp;
    }

    private RefreshToken() { }

    public Guid RefreshTokenId { get; private init; }
    public Guid UserId { get; private init; }
    public string TokenHash { get; private init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private init; }
    public DateTime CreatedAtUtc { get; private init; }
    public DateTime? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private init; }
    public string? RevokedByIp { get; private set; }

    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc.HasValue;
    public bool IsActive(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc,
        string? createdByIp = null)
    {
        if (userId == Guid.Empty) throw new IdentityDomainException("UserId is required.");
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new IdentityDomainException("TokenHash is required.");
        if (tokenHash.Length > 500) throw new IdentityDomainException("TokenHash cannot exceed 500 characters.");
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        if (expiresAtUtc <= createdAtUtc) throw new IdentityDomainException("Refresh token expiration must be after creation.");

        createdByIp = NormalizeIp(createdByIp, nameof(createdByIp));
        return new RefreshToken(
            Guid.CreateVersion7(), userId, tokenHash, expiresAtUtc, createdAtUtc, createdByIp);
    }

    public void Revoke(DateTime revokedAtUtc, string? revokedByIp = null)
    {
        EnsureUtc(revokedAtUtc, nameof(revokedAtUtc));
        if (IsRevoked) return;

        RevokedAtUtc = revokedAtUtc;
        RevokedByIp = NormalizeIp(revokedByIp, nameof(revokedByIp));
    }

    public void ReplaceWith(Guid replacementTokenId, DateTime revokedAtUtc, string? revokedByIp = null)
    {
        if (replacementTokenId == Guid.Empty)
            throw new IdentityDomainException("ReplacementTokenId is required.");
        if (replacementTokenId == RefreshTokenId)
            throw new IdentityDomainException("A refresh token cannot replace itself.");
        if (IsRevoked)
            throw new IdentityDomainException("A revoked refresh token cannot be replaced.");

        Revoke(revokedAtUtc, revokedByIp);
        ReplacedByTokenId = replacementTokenId;
    }

    private static void EnsureUtc(DateTime value, string name)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new IdentityDomainException($"{name} must be UTC.");
    }

    private static string? NormalizeIp(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > 64) throw new IdentityDomainException($"{name} cannot exceed 64 characters.");
        return trimmed;
    }
}
