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

public sealed class LogoutEndpointTests
{
    [Fact]
    public async Task ActiveToken_IsRevokedAndCannotBeRefreshed()
    {
        await using var factory = new IdentityApiFactory();
        var login = await LoginAsync(factory);
        var client = factory.CreateClient();

        var logout = await client.PostAsJsonAsync(
            "/api/identity/auth/logout", new LogoutRequest(login.RefreshToken));
        var refresh = await client.PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest(login.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>()
            .RefreshTokens.SingleAsync();
        Assert.True(stored.IsRevoked);
    }

    [Fact]
    public async Task RepeatedLogout_ReturnsSafeSuccess()
    {
        await using var factory = new IdentityApiFactory();
        var login = await LoginAsync(factory);
        var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/api/identity/auth/logout", new LogoutRequest(login.RefreshToken));
        var repeated = await client.PostAsJsonAsync(
            "/api/identity/auth/logout", new LogoutRequest(login.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
    }

    [Fact]
    public async Task UnknownToken_ReturnsSafeSuccess()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/logout", new LogoutRequest("unknown-token"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task MissingToken_ReturnsBadRequest()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/logout", new LogoutRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<LoginResponse> LoginAsync(IdentityApiFactory factory)
    {
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var services = scope.ServiceProvider;
            var schoolId = factory.SchoolId;
            var user = User.Create(
                schoolId, "admin", "ADMIN", "admin@smartschool.com", "ADMIN@SMARTSCHOOL.COM",
                services.GetRequiredService<IPasswordHasher>().Hash("StrongPassword123!"), "Ahmed", "Mohamed");
            var dbContext = services.GetRequiredService<IdentityDbContext>();
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login",
            new LoginRequest(factory.SchoolId, "admin", "StrongPassword123!"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private sealed record LoginRequest(Guid SchoolId, string UsernameOrEmail, string Password);
    private sealed record LoginResponse(
        string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc,
        Guid UserId, string Username, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
    private sealed record LogoutRequest(string RefreshToken);
    private sealed record RefreshRequest(string RefreshToken);

    private sealed class IdentityApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();
        public Guid SchoolId { get; } = Guid.NewGuid();

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
