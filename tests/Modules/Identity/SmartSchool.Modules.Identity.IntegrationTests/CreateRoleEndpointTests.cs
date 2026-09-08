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

public sealed class CreateRoleEndpointTests
{
    [Fact]
    public async Task ValidRequest_ReturnsCreated()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/identity/roles", ValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateRoleResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.RoleId);
    }

    [Fact]
    public async Task DuplicateRole_ReturnsConflict()
    {
        await using var factory = new IdentityApiFactory();
        var client = factory.CreateClient();
        var request = ValidRequest();
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/identity/roles", request)).StatusCode);

        var response = await client.PostAsJsonAsync(
            "/api/identity/roles",
            request with { Name = " teacher ", Description = null });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("identity.role_name_exists", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task InvalidName_ReturnsValidationError()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/roles",
            ValidRequest() with { Name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Request validation failed", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EmptySchoolId_ReturnsValidationError()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/roles",
            ValidRequest() with { SchoolId = Guid.Empty });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("SchoolId must not be empty", await response.Content.ReadAsStringAsync());
    }

    private static CreateRoleRequest ValidRequest() =>
        new(Guid.NewGuid(), "Teacher", "School teacher role");

    private sealed record CreateRoleRequest(Guid SchoolId, string Name, string? Description);
    private sealed record CreateRoleResponse(Guid RoleId);

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
