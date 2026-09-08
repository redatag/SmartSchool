using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;

namespace SmartSchool.Modules.Identity.IntegrationTests;

public sealed class CreateUserEndpointTests
{
    [Fact]
    public async Task ValidUser_ReturnsCreated()
    {
        await using var factory = new IdentityApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/identity/users", ValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateUserResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.UserId);
    }

    [Fact]
    public async Task DuplicateUsername_ReturnsConflict()
    {
        await using var factory = new IdentityApiFactory();
        var client = factory.CreateClient();
        var request = ValidRequest();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/identity/users", request)).StatusCode);

        var response = await client.PostAsJsonAsync("/api/identity/users", request with { Email = "other@smartschool.com" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("identity.username_exists", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DuplicateEmail_ReturnsConflict()
    {
        await using var factory = new IdentityApiFactory();
        var client = factory.CreateClient();
        var request = ValidRequest();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/identity/users", request)).StatusCode);

        var response = await client.PostAsJsonAsync("/api/identity/users", request with { Username = "other-admin" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("identity.email_exists", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("invalid-email", "StrongPassword123!")]
    [InlineData("admin@smartschool.com", "short")]
    public async Task InvalidRequest_ReturnsValidationError(string email, string password)
    {
        await using var factory = new IdentityApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/users", ValidRequest() with { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Request validation failed", await response.Content.ReadAsStringAsync());
    }

    private static CreateUserRequest ValidRequest() => new(
        Guid.NewGuid(), "admin", "admin@smartschool.com", "StrongPassword123!", "Ahmed", "Mohamed", "0500000000");

    private sealed record CreateUserRequest(
        Guid SchoolId, string Username, string Email, string Password, string FirstName, string LastName, string? PhoneNumber);
    private sealed record CreateUserResponse(Guid UserId);

    private sealed class IdentityApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
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
