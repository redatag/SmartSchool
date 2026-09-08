using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class RefreshTokenTests
{
    [Fact]
    public void Create_CreatesActiveVersion7Token()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", now.AddDays(7), now);

        Assert.Equal(7, token.RefreshTokenId.Version);
        Assert.True(token.IsActive(now));
        Assert.False(token.IsExpired(now));
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void Revoke_MakesTokenInactive()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", now.AddDays(7), now);

        token.Revoke(now.AddMinutes(1), " 127.0.0.1 ");

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive(now.AddMinutes(1)));
        Assert.Equal("127.0.0.1", token.RevokedByIp);
    }

    [Fact]
    public void ReplaceWith_RevokesAndLinksReplacement()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", now.AddDays(7), now);
        var replacementId = Guid.CreateVersion7();

        token.ReplaceWith(replacementId, now.AddMinutes(1));

        Assert.True(token.IsRevoked);
        Assert.Equal(replacementId, token.ReplacedByTokenId);
    }

    [Fact]
    public void IsExpired_WhenExpirationReached()
    {
        var now = DateTime.UtcNow;
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", now.AddMinutes(1), now);

        Assert.True(token.IsExpired(now.AddMinutes(1)));
        Assert.False(token.IsActive(now.AddMinutes(1)));
    }
}
