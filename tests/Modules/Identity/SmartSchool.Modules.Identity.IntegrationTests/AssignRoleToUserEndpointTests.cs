using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSchool.Modules.Identity.Domain;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;

namespace SmartSchool.Modules.Identity.IntegrationTests;

public sealed class AssignRoleToUserEndpointTests
{
    [Fact]
    public async Task ValidAssignment_ReturnsNoContent()
    {
        await using var factory = new IdentityApiFactory();
        var (userId, roleId) = await SeedUserAndRoleAsync(factory);

        var response = await factory.CreateClientWithPermission("roles.manage").PostAsync(Endpoint(userId, roleId), content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ValidAssignment_PersistsUserRole()
    {
        await using var factory = new IdentityApiFactory();
        var (userId, roleId) = await SeedUserAndRoleAsync(factory);
        var client = factory.CreateClientWithPermission("roles.manage");

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync(Endpoint(userId, roleId), null)).StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<IdentityDbContext>()
            .Users.Include(candidate => candidate.UserRoles)
            .SingleAsync(candidate => candidate.UserId == userId);
        var userRole = Assert.Single(user.UserRoles);
        Assert.Equal(roleId, userRole.RoleId);
    }

    [Fact]
    public async Task DuplicateAssignment_ReturnsConflict()
    {
        await using var factory = new IdentityApiFactory();
        var (userId, roleId) = await SeedUserAndRoleAsync(factory);
        var client = factory.CreateClientWithPermission("roles.manage");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync(Endpoint(userId, roleId), null)).StatusCode);

        var response = await client.PostAsync(Endpoint(userId, roleId), null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("identity.role_already_assigned", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownUser_ReturnsNotFound()
    {
        await using var factory = new IdentityApiFactory();
        var (_, roleId) = await SeedUserAndRoleAsync(factory);

        var response = await factory.CreateClientWithPermission("roles.manage").PostAsync(Endpoint(Guid.NewGuid(), roleId), null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("identity.user_not_found", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownRole_ReturnsNotFound()
    {
        await using var factory = new IdentityApiFactory();
        var (userId, _) = await SeedUserAndRoleAsync(factory);

        var response = await factory.CreateClientWithPermission("roles.manage").PostAsync(Endpoint(userId, Guid.NewGuid()), null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("identity.role_not_found", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CrossSchoolAssignment_ReturnsConflict()
    {
        await using var factory = new IdentityApiFactory();
        var (userId, roleId) = await SeedUserAndRoleAsync(factory, sameSchool: false);

        var response = await factory.CreateClientWithPermission("roles.manage").PostAsync(Endpoint(userId, roleId), null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("identity.role_school_mismatch", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EmptyIds_ReturnValidationErrors()
    {
        await using var factory = new IdentityApiFactory();
        var client = factory.CreateClientWithPermission("roles.manage");

        var emptyUserResponse = await client.PostAsync(Endpoint(Guid.Empty, Guid.NewGuid()), null);
        var emptyRoleResponse = await client.PostAsync(Endpoint(Guid.NewGuid(), Guid.Empty), null);

        Assert.Equal(HttpStatusCode.BadRequest, emptyUserResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, emptyRoleResponse.StatusCode);
    }

    private static string Endpoint(Guid userId, Guid roleId) =>
        $"/api/identity/users/{userId}/roles/{roleId}";

    private static async Task<(Guid UserId, Guid RoleId)> SeedUserAndRoleAsync(
        IdentityApiFactory factory,
        bool sameSchool = true)
    {
        var userSchoolId = Guid.NewGuid();
        var roleSchoolId = sameSchool ? userSchoolId : Guid.NewGuid();
        var user = User.Create(
            userSchoolId,
            "admin",
            "ADMIN",
            "admin@smartschool.com",
            "ADMIN@SMARTSCHOOL.COM",
            "hashed-password",
            "Ahmed",
            "Mohamed");
        var role = Role.Create(roleSchoolId, "Teacher");

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync();
        return (user.UserId, role.RoleId);
    }

    private sealed class IdentityApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            IdentityIntegrationTestAuthentication.ConfigureJwt(builder);
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
