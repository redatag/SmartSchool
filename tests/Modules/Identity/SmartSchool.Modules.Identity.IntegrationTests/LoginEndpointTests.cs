using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
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

public sealed class LoginEndpointTests
{
    [Fact]
    public async Task ValidCredentials_ReturnOkWithAccessToken()
    {
        await using var factory = new IdentityApiFactory();
        var seeded = await SeedUserAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login",
            ValidRequest(seeded.SchoolId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal(seeded.UserId, body.UserId);
        Assert.Equal("admin", body.Username);
        Assert.Equal(["SchoolAdmin"], body.Roles);
        Assert.Empty(body.Permissions);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        Assert.Equal("SmartSchool.Tests", token.Issuer);
        Assert.Contains("SmartSchool.Tests.Client", token.Audiences);
        Assert.Equal(seeded.UserId.ToString(), token.Subject);
        Assert.Equal(seeded.SchoolId.ToString(), token.Claims.Single(x => x.Type == "school_id").Value);
        Assert.Equal("admin", token.Claims.Single(x => x.Type == "username").Value);
        Assert.Equal("SchoolAdmin", token.Claims.Single(x => x.Type == ClaimTypes.Role).Value);
        Assert.InRange(body.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
    }

    [Fact]
    public async Task IncorrectPassword_ReturnsUnauthorized()
    {
        await using var factory = new IdentityApiFactory();
        var seeded = await SeedUserAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login",
            ValidRequest(seeded.SchoolId) with { Password = "incorrect" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.invalid_credentials", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownUser_ReturnsUnauthorized()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login",
            ValidRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("identity.invalid_credentials", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InactiveUser_ReturnsForbidden()
    {
        await using var factory = new IdentityApiFactory();
        var seeded = await SeedUserAsync(factory, user => user.Deactivate());

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login",
            ValidRequest(seeded.SchoolId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("identity.account_inactive", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ValidEmailLogin_ReturnsOk()
    {
        await using var factory = new IdentityApiFactory();
        var seeded = await SeedUserAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login",
            ValidRequest(seeded.SchoolId) with { UsernameOrEmail = "ADMIN@smartschool.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequest_ReturnsBadRequest()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login",
            new LoginRequest(Guid.Empty, "", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Request validation failed", await response.Content.ReadAsStringAsync());
    }

    private static LoginRequest ValidRequest(Guid schoolId) =>
        new(schoolId, "admin", "StrongPassword123!");

    private static async Task<SeededIdentity> SeedUserAsync(
        IdentityApiFactory factory,
        Action<User>? configureUser = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var schoolId = Guid.NewGuid();
        var passwordHash = services.GetRequiredService<IPasswordHasher>().Hash("StrongPassword123!");
        var user = User.Create(
            schoolId,
            "admin",
            "ADMIN",
            "admin@smartschool.com",
            "ADMIN@SMARTSCHOOL.COM",
            passwordHash,
            "Ahmed",
            "Mohamed");
        var role = Role.Create(schoolId, "SchoolAdmin");
        user.AssignRole(role.RoleId);
        configureUser?.Invoke(user);

        var dbContext = services.GetRequiredService<IdentityDbContext>();
        dbContext.Roles.Add(role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return new SeededIdentity(schoolId, user.UserId);
    }

    private sealed record SeededIdentity(Guid SchoolId, Guid UserId);
    private sealed record LoginRequest(Guid SchoolId, string UsernameOrEmail, string Password);
    private sealed record LoginResponse(
        string AccessToken,
        DateTime ExpiresAtUtc,
        Guid UserId,
        string Username,
        IReadOnlyCollection<string> Roles,
        IReadOnlyCollection<string> Permissions);

    private sealed class IdentityApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "SmartSchool.Tests",
                    ["Jwt:Audience"] = "SmartSchool.Tests.Client",
                    ["Jwt:Key"] = "test-only-signing-key-with-at-least-thirty-two-bytes",
                    ["Jwt:AccessTokenExpirationMinutes"] = "30"
                });
            });
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
