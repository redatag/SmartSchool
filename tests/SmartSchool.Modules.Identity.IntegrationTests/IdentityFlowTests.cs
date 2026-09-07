using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Infrastructure.Authentication;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;

namespace SmartSchool.Modules.Identity.IntegrationTests;

public sealed class IdentityFlowTests
{
    [Fact]
    public async Task CreateLoginAndRefresh_PersistsAndRotatesCredentials()
    {
        await using var dbContext = CreateDbContext();
        var users = new UserRepository(dbContext);
        var passwordHasher = new PasswordHasher();
        var clock = new FixedClock(new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero));
        var tokens = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "tests",
            Audience = "tests",
            Key = "integration-test-signing-key-that-is-long-enough"
        }), clock);

        var create = new CreateUserHandler(users, passwordHasher, dbContext);
        var created = await create.HandleAsync(
            new("learner@school.test", "Test Learner", "StrongPass1"), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var login = new LoginHandler(users, passwordHasher, tokens, clock, dbContext);
        var authenticated = await login.HandleAsync(
            new("learner@school.test", "StrongPass1"), CancellationToken.None);
        Assert.True(authenticated.IsSuccess);
        Assert.NotEmpty(authenticated.Value.AccessToken);

        var refresh = new RefreshTokenHandler(users, tokens, clock, dbContext);
        var rotated = await refresh.HandleAsync(
            new(authenticated.Value.RefreshToken), CancellationToken.None);
        Assert.True(rotated.IsSuccess);
        Assert.NotEqual(authenticated.Value.RefreshToken, rotated.Value.RefreshToken);
        Assert.True((await refresh.HandleAsync(
            new(authenticated.Value.RefreshToken), CancellationToken.None)).IsFailure);
    }

    [Fact]
    public async Task SavingAggregate_WritesOutboxMessage()
    {
        await using var dbContext = CreateDbContext();
        var users = new UserRepository(dbContext);
        var handler = new CreateUserHandler(users, new PasswordHasher(), dbContext);

        var result = await handler.HandleAsync(
            new("events@school.test", "Event User", "StrongPass1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(await dbContext.OutboxMessages.ToListAsync());
    }

    private static IdentityDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(
                Guid.NewGuid().ToString(),
                options => options.EnableNullChecks(false))
            .Options);

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
    }
}
