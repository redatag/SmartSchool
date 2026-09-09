using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.UnitTests;

public sealed class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_ActiveToken_RevokesAndSaves()
    {
        var fixture = new Fixture();

        var result = await fixture.HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.True(fixture.Token.IsRevoked);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_RepeatedLogout_IsIdempotent()
    {
        var fixture = new Fixture();

        await fixture.HandleAsync();
        var repeated = await fixture.HandleAsync();

        Assert.True(repeated.IsSuccess);
        Assert.True(fixture.Token.IsRevoked);
        Assert.Equal(1, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsSafeSuccessWithoutSaving()
    {
        var fixture = new Fixture(tokenExists: false);

        var result = await fixture.HandleAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, fixture.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Handle_MissingToken_ReturnsValidationError()
    {
        var fixture = new Fixture();

        var result = await fixture.Handler.HandleAsync(new LogoutCommand(""), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Single(result.ValidationErrors);
    }

    private sealed class Fixture
    {
        public Fixture(bool tokenExists = true)
        {
            var now = DateTime.UtcNow;
            Token = RefreshToken.Create(Guid.NewGuid(), "token-hash", now.AddDays(7), now);
            UnitOfWork = new FakeUnitOfWork();
            Handler = new LogoutCommandHandler(
                new LogoutCommandValidator(),
                new FakeRefreshTokenProvider(),
                new FakeRefreshTokenRepository(tokenExists ? Token : null),
                UnitOfWork);
        }

        public RefreshToken Token { get; }
        public FakeUnitOfWork UnitOfWork { get; }
        public LogoutCommandHandler Handler { get; }
        public Task<LogoutResult> HandleAsync() =>
            Handler.HandleAsync(new LogoutCommand("raw-token"), CancellationToken.None);
    }

    private sealed class FakeRefreshTokenProvider : IRefreshTokenProvider
    {
        public GeneratedRefreshToken Generate() => throw new NotSupportedException();
        public string Hash(string token) => "token-hash";
    }

    private sealed class FakeRefreshTokenRepository(RefreshToken? token) : IRefreshTokenRepository
    {
        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(token?.TokenHash == tokenHash ? token : null);
        public void Add(RefreshToken refreshToken) => throw new NotSupportedException();
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
