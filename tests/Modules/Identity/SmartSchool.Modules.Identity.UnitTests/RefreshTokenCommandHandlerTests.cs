using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_ActiveToken_RotatesAndReturnsNewTokens()
    {
        var fixture = new Fixture();

        var result = await fixture.HandleAsync();

        Assert.Equal(RefreshTokenStatus.Success, result.Status);
        Assert.Equal("new-access-token", result.Tokens?.AccessToken);
        Assert.Equal("new-refresh-token", result.Tokens?.RefreshToken);
        Assert.True(fixture.CurrentToken.IsRevoked);
        Assert.Equal(fixture.Repository.AddedToken?.RefreshTokenId, fixture.CurrentToken.ReplacedByTokenId);
        Assert.Equal("new-refresh-token-hash", fixture.Repository.AddedToken?.TokenHash);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsUnauthorized()
    {
        var fixture = new Fixture(tokenExists: false);

        var result = await fixture.HandleAsync();

        Assert.Equal(RefreshTokenStatus.Unauthorized, result.Status);
        Assert.Equal("identity.invalid_refresh_token", result.ErrorCode);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsUnauthorized()
    {
        var fixture = new Fixture(expired: true);

        var result = await fixture.HandleAsync();

        Assert.Equal(RefreshTokenStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task Handle_RevokedToken_ReturnsUnauthorized()
    {
        var fixture = new Fixture();
        fixture.CurrentToken.Revoke(DateTime.UtcNow);

        var result = await fixture.HandleAsync();

        Assert.Equal(RefreshTokenStatus.Unauthorized, result.Status);
    }

    [Fact]
    public async Task Handle_ReusedRotatedToken_ReturnsUnauthorized()
    {
        var fixture = new Fixture();
        Assert.Equal(RefreshTokenStatus.Success, (await fixture.HandleAsync()).Status);

        var reused = await fixture.HandleAsync();

        Assert.Equal(RefreshTokenStatus.Unauthorized, reused.Status);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_HashesProvidedTokenBeforeLookup()
    {
        var fixture = new Fixture();

        await fixture.HandleAsync();

        Assert.Equal("stored-token-hash", fixture.Repository.LastLookupHash);
        Assert.NotEqual("stored-token", fixture.Repository.LastLookupHash);
    }

    private sealed class Fixture
    {
        public Fixture(bool tokenExists = true, bool expired = false)
        {
            User = User.Create(Guid.NewGuid(), "admin", "ADMIN", "admin@school.test", "ADMIN@SCHOOL.TEST",
                "hash", "Admin", "User");
            var now = DateTime.UtcNow;
            CurrentToken = RefreshToken.Create(
                User.UserId,
                "stored-token-hash",
                expired ? now.AddMinutes(-1) : now.AddDays(1),
                expired ? now.AddDays(-1) : now);
            Repository = new FakeRefreshTokenRepository(tokenExists ? CurrentToken : null);
            UnitOfWork = new FakeUnitOfWork();
            Handler = new RefreshTokenCommandHandler(
                new RefreshTokenCommandValidator(),
                new FakeRefreshTokenProvider(),
                Repository,
                new FakeUserRepository(User),
                new FakeAccessReader(),
                new FakeAccessTokenProvider(),
                UnitOfWork);
        }

        public User User { get; }
        public RefreshToken CurrentToken { get; }
        public FakeRefreshTokenRepository Repository { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        private RefreshTokenCommandHandler Handler { get; }

        public Task<RefreshTokenResult> HandleAsync() =>
            Handler.HandleAsync(new RefreshTokenCommand("stored-token"), CancellationToken.None);
    }

    private sealed class FakeRefreshTokenProvider : IRefreshTokenProvider
    {
        public GeneratedRefreshToken Generate()
        {
            var now = DateTime.UtcNow;
            return new("new-refresh-token", "new-refresh-token-hash", now.AddDays(7), now);
        }

        public string Hash(string token) => token == "stored-token" ? "stored-token-hash" : "unknown-hash";
    }

    private sealed class FakeRefreshTokenRepository(RefreshToken? token) : IRefreshTokenRepository
    {
        public string? LastLookupHash { get; private set; }
        public RefreshToken? AddedToken { get; private set; }

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            LastLookupHash = tokenHash;
            return Task.FromResult(token?.TokenHash == tokenHash ? token : null);
        }

        public void Add(RefreshToken refreshToken) => AddedToken = refreshToken;
    }

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserId == userId ? user : null);
        public Task<User?> GetByUsernameOrEmailAsync(Guid schoolId, string normalizedUsernameOrEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> UsernameExistsAsync(Guid schoolId, string normalizedUsername, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(Guid schoolId, string normalizedEmail, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Add(User addedUser) => throw new NotSupportedException();
    }

    private sealed class FakeAccessReader : IUserAccessReader
    {
        public Task<UserAccess> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(new UserAccess(["Admin"], ["users.manage"]));
    }

    private sealed class FakeAccessTokenProvider : ITokenProvider
    {
        public AccessTokenResult GenerateAccessToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions) =>
            new("new-access-token", DateTime.UtcNow.AddMinutes(30));
    }

    public sealed class FakeUnitOfWork : IIdentityUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
