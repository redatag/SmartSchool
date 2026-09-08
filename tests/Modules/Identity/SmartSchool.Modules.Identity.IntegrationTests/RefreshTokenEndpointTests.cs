using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;

namespace SmartSchool.Modules.Identity.IntegrationTests;

public sealed class RefreshTokenEndpointTests
{
    [Fact]
    public async Task ActiveToken_RotatesTokensAndRevokesOldToken()
    {
        await using var factory = new IdentityApiFactory();
        var login = await LoginAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest(login.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshed = await response.Content.ReadFromJsonAsync<RefreshResponse>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(login.AccessToken, refreshed.AccessToken);
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var tokens = await dbContext.RefreshTokens.OrderBy(x => x.CreatedAtUtc).ToArrayAsync();
        Assert.Equal(2, tokens.Length);
        Assert.True(tokens[0].IsRevoked);
        Assert.Equal(tokens[1].RefreshTokenId, tokens[0].ReplacedByTokenId);
        Assert.DoesNotContain(tokens, x => x.TokenHash == login.RefreshToken || x.TokenHash == refreshed.RefreshToken);
    }

    [Fact]
    public async Task InvalidToken_ReturnsUnauthorized()
    {
        await using var factory = new IdentityApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest("not-a-valid-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.invalid_refresh_token", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExpiredToken_ReturnsUnauthorized()
    {
        await using var factory = new IdentityApiFactory();
        var rawToken = await SeedExpiredTokenAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest(rawToken));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RotatedToken_CannotBeReused()
    {
        await using var factory = new IdentityApiFactory();
        var login = await LoginAsync(factory);
        var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest(login.RefreshToken));
        var reuse = await client.PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest(login.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task MissingToken_ReturnsBadRequest()
    {
        await using var factory = new IdentityApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<LoginResponse> LoginAsync(IdentityApiFactory factory)
    {
        var schoolId = await SeedUserAsync(factory);
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login",
            new LoginRequest(schoolId, "admin", "StrongPassword123!"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private static async Task<string> SeedExpiredTokenAsync(IdentityApiFactory factory)
    {
        var schoolId = await SeedUserAsync(factory);
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        var userId = await dbContext.Users.Where(x => x.SchoolId == schoolId).Select(x => x.UserId).SingleAsync();
        const string rawToken = "expired-raw-token";
        var hash = services.GetRequiredService<IRefreshTokenProvider>().Hash(rawToken);
        var now = DateTime.UtcNow;
        dbContext.RefreshTokens.Add(RefreshToken.Create(userId, hash, now.AddMinutes(-1), now.AddDays(-1)));
        await dbContext.SaveChangesAsync();
        return rawToken;
    }

    private static async Task<Guid> SeedUserAsync(IdentityApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var schoolId = Guid.NewGuid();
        var user = User.Create(
            schoolId, "admin", "ADMIN", "admin@smartschool.com", "ADMIN@SMARTSCHOOL.COM",
            services.GetRequiredService<IPasswordHasher>().Hash("StrongPassword123!"), "Ahmed", "Mohamed");
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return schoolId;
    }

    private sealed record LoginRequest(Guid SchoolId, string UsernameOrEmail, string Password);
    private sealed record LoginResponse(
        string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc,
        Guid UserId, string Username, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
    private sealed record RefreshRequest(string RefreshToken);
    private sealed record RefreshResponse(
        string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);

    private sealed class IdentityApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "SmartSchool.Tests",
                    ["Jwt:Audience"] = "SmartSchool.Tests.Client",
                    ["Jwt:Key"] = "test-only-signing-key-with-at-least-thirty-two-bytes",
                    ["Jwt:AccessTokenExpirationMinutes"] = "30",
                    ["Jwt:RefreshTokenExpirationDays"] = "7"
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<IdentityDbContext>>();
                services.RemoveAll<IdentityDbContext>();
                services.AddDbContext<IdentityDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName, database => database.EnableNullChecks(false)));
            });
        }
    }
}
