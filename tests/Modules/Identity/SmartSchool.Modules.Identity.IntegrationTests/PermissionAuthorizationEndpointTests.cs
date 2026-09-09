using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;
using SmartSchool.Modules.Identity.Infrastructure.Persistence;

namespace SmartSchool.Modules.Identity.IntegrationTests;

public sealed class PermissionAuthorizationEndpointTests
{
    [Fact]
    public async Task RequiredPermission_AllowsRequest()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClientWithPermission(IdentityPermissionCodes.UsersManage)
            .PostAsJsonAsync("/api/identity/users", ValidCreateUserRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task MissingPermission_ReturnsForbidden()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClientWithPermission(IdentityPermissionCodes.UsersView)
            .PostAsJsonAsync("/api/identity/users", ValidCreateUserRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        await using var factory = new IdentityApiFactory();

        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/identity/users", ValidCreateUserRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RolePermission_IsResolvedAndIncludedInAccessToken()
    {
        await using var factory = new IdentityApiFactory();
        var schoolId = await SeedUserWithPermissionAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/identity/auth/login",
            new LoginRequest(schoolId, "admin", "StrongPassword123!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.Contains(IdentityPermissionCodes.UsersManage, body.Permissions);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        Assert.Contains(jwt.Claims, claim =>
            claim.Type == "permission" && claim.Value == IdentityPermissionCodes.UsersManage);
    }

    private static async Task<Guid> SeedUserWithPermissionAsync(IdentityApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var schoolId = Guid.NewGuid();
        var permission = Permission.Create(IdentityPermissionCodes.UsersManage, "Manage users", "Identity");
        var role = Role.Create(schoolId, "Administrator");
        role.GrantPermission(permission.PermissionId);
        var user = User.Create(
            schoolId, "admin", "ADMIN", "admin@smartschool.com", "ADMIN@SMARTSCHOOL.COM",
            services.GetRequiredService<IPasswordHasher>().Hash("StrongPassword123!"), "Ahmed", "Mohamed");
        user.AssignRole(role.RoleId);

        var dbContext = services.GetRequiredService<IdentityDbContext>();
        dbContext.Permissions.Add(permission);
        dbContext.Roles.Add(role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return schoolId;
    }

    private static CreateUserRequest ValidCreateUserRequest() => new(
        Guid.NewGuid(), "admin", "admin@smartschool.com", "StrongPassword123!", "Ahmed", "Mohamed", null);

    private sealed record CreateUserRequest(
        Guid SchoolId, string Username, string Email, string Password,
        string FirstName, string LastName, string? PhoneNumber);
    private sealed record LoginRequest(Guid SchoolId, string UsernameOrEmail, string Password);
    private sealed record LoginResponse(
        string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc,
        Guid UserId, string Username, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);

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
