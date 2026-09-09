using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
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

public sealed class IdentityLifecycleEndToEndTests
{
    [Fact]
    public async Task CompleteIdentityLifecycle_WorksThroughApi()
    {
        await using var factory = new IdentityApiFactory();
        var schoolId = Guid.NewGuid();
        await SeedBootstrapAdministratorAsync(factory, schoolId);
        var anonymousClient = factory.CreateClient();

        var administratorLogin = await LoginAsync(
            anonymousClient, schoolId, "bootstrap", "BootstrapPassword123!");
        var administratorClient = factory.CreateClient().UseBearerToken(administratorLogin.AccessToken);

        var userResponse = await administratorClient.PostAsJsonAsync(
            "/api/identity/users",
            NewUser(schoolId, "teacher", "teacher@smartschool.test", "TeacherPassword123!"));
        Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);
        var userJson = await userResponse.Content.ReadAsStringAsync();
        var createdUser = await userResponse.Content.ReadFromJsonAsync<CreateUserResponse>();
        Assert.NotNull(createdUser);
        Assert.DoesNotContain("password", userJson, StringComparison.OrdinalIgnoreCase);

        var roleResponse = await administratorClient.PostAsJsonAsync(
            "/api/identity/roles",
            new CreateRoleRequest(schoolId, "SchoolAdmin", "Identity lifecycle test role"));
        Assert.Equal(HttpStatusCode.Created, roleResponse.StatusCode);
        var createdRole = await roleResponse.Content.ReadFromJsonAsync<CreateRoleResponse>();
        Assert.NotNull(createdRole);

        var assignmentResponse = await administratorClient.PostAsync(
            $"/api/identity/users/{createdUser.UserId}/roles/{createdRole.RoleId}", null);
        Assert.Equal(HttpStatusCode.NoContent, assignmentResponse.StatusCode);
        await GrantPermissionAsync(factory, createdRole.RoleId, IdentityPermissionCodes.UsersManage);

        var userLogin = await LoginAsync(anonymousClient, schoolId, "teacher", "TeacherPassword123!");
        Assert.Equal(createdUser.UserId, userLogin.UserId);
        Assert.Contains("SchoolAdmin", userLogin.Roles);
        Assert.Contains(IdentityPermissionCodes.UsersManage, userLogin.Permissions);
        Assert.False(string.IsNullOrWhiteSpace(userLogin.RefreshToken));
        Assert.True(userLogin.ExpiresAtUtc > DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(userLogin.AccessToken);
        Assert.Equal(createdUser.UserId.ToString(), jwt.Subject);
        Assert.Equal(schoolId.ToString(), jwt.Claims.Single(x => x.Type == "school_id").Value);
        Assert.Equal("SchoolAdmin", jwt.Claims.Single(x => x.Type == ClaimTypes.Role).Value);
        Assert.Equal(
            IdentityPermissionCodes.UsersManage,
            jwt.Claims.Single(x => x.Type == "permission").Value);

        var authorizedClient = factory.CreateClient().UseBearerToken(userLogin.AccessToken);
        var authorizedResponse = await authorizedClient.PostAsJsonAsync(
            "/api/identity/users",
            NewUser(schoolId, "authorized-created", "authorized-created@smartschool.test"));
        Assert.Equal(HttpStatusCode.Created, authorizedResponse.StatusCode);

        var limitedUserId = await CreateLimitedUserAsync(administratorClient, schoolId);
        var limitedLogin = await LoginAsync(anonymousClient, schoolId, "limited", "LimitedPassword123!");
        Assert.Equal(limitedUserId, limitedLogin.UserId);
        var forbiddenResponse = await factory.CreateClient().UseBearerToken(limitedLogin.AccessToken)
            .PostAsJsonAsync(
                "/api/identity/users",
                NewUser(schoolId, "forbidden-created", "forbidden-created@smartschool.test"));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        var unauthenticatedResponse = await anonymousClient.PostAsJsonAsync(
            "/api/identity/users",
            NewUser(schoolId, "anonymous-created", "anonymous-created@smartschool.test"));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedResponse.StatusCode);

        var refreshResponse = await anonymousClient.PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest(userLogin.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponse>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(userLogin.AccessToken, refreshed.AccessToken);
        Assert.NotEqual(userLogin.RefreshToken, refreshed.RefreshToken);
        await AssertTokenRotatedAndHashedAsync(factory, userLogin.RefreshToken, refreshed.RefreshToken);

        var reuseResponse = await anonymousClient.PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest(userLogin.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        var refreshedAuthorizedResponse = await factory.CreateClient().UseBearerToken(refreshed.AccessToken)
            .PostAsJsonAsync(
                "/api/identity/users",
                NewUser(schoolId, "refreshed-created", "refreshed-created@smartschool.test"));
        Assert.Equal(HttpStatusCode.Created, refreshedAuthorizedResponse.StatusCode);

        var logoutResponse = await anonymousClient.PostAsJsonAsync(
            "/api/identity/auth/logout", new LogoutRequest(refreshed.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        var loggedOutRefresh = await anonymousClient.PostAsJsonAsync(
            "/api/identity/auth/refresh", new RefreshRequest(refreshed.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, loggedOutRefresh.StatusCode);
    }

    private static async Task SeedBootstrapAdministratorAsync(IdentityApiFactory factory, Guid schoolId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var usersPermission = Permission.Create(IdentityPermissionCodes.UsersManage, "Manage users", "Identity");
        var rolesPermission = Permission.Create(IdentityPermissionCodes.RolesManage, "Manage roles", "Identity");
        var role = Role.Create(schoolId, "BootstrapAdministrator", isSystemRole: true);
        role.GrantPermission(usersPermission.PermissionId);
        role.GrantPermission(rolesPermission.PermissionId);
        var user = User.Create(
            schoolId, "bootstrap", "BOOTSTRAP", "bootstrap@smartschool.test", "BOOTSTRAP@SMARTSCHOOL.TEST",
            services.GetRequiredService<IPasswordHasher>().Hash("BootstrapPassword123!"), "Bootstrap", "Admin");
        user.AssignRole(role.RoleId);

        var dbContext = services.GetRequiredService<IdentityDbContext>();
        dbContext.Permissions.AddRange(usersPermission, rolesPermission);
        dbContext.Roles.Add(role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
    }

    private static async Task GrantPermissionAsync(
        IdentityApiFactory factory,
        Guid roleId,
        string permissionCode)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var permissionId = await dbContext.Permissions
            .Where(x => x.Code == permissionCode)
            .Select(x => x.PermissionId)
            .FirstAsync();
        var role = await dbContext.Roles.Include(x => x.RolePermissions).SingleAsync(x => x.RoleId == roleId);
        role.GrantPermission(permissionId);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<Guid> CreateLimitedUserAsync(HttpClient administratorClient, Guid schoolId)
    {
        var userResponse = await administratorClient.PostAsJsonAsync(
            "/api/identity/users",
            NewUser(schoolId, "limited", "limited@smartschool.test", "LimitedPassword123!"));
        userResponse.EnsureSuccessStatusCode();
        var userId = (await userResponse.Content.ReadFromJsonAsync<CreateUserResponse>())!.UserId;

        var roleResponse = await administratorClient.PostAsJsonAsync(
            "/api/identity/roles",
            new CreateRoleRequest(schoolId, "LimitedUser", "Role without management permissions"));
        roleResponse.EnsureSuccessStatusCode();
        var roleId = (await roleResponse.Content.ReadFromJsonAsync<CreateRoleResponse>())!.RoleId;
        var assignmentResponse = await administratorClient.PostAsync(
            $"/api/identity/users/{userId}/roles/{roleId}", null);
        assignmentResponse.EnsureSuccessStatusCode();
        return userId;
    }

    private static async Task<LoginResponse> LoginAsync(
        HttpClient client,
        Guid schoolId,
        string username,
        string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/identity/auth/login", new LoginRequest(schoolId, username, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    private static async Task AssertTokenRotatedAndHashedAsync(
        IdentityApiFactory factory,
        string oldRawToken,
        string newRawToken)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var provider = services.GetRequiredService<IRefreshTokenProvider>();
        var dbContext = services.GetRequiredService<IdentityDbContext>();
        var oldToken = await dbContext.RefreshTokens.SingleAsync(x => x.TokenHash == provider.Hash(oldRawToken));
        var newToken = await dbContext.RefreshTokens.SingleAsync(x => x.TokenHash == provider.Hash(newRawToken));
        Assert.True(oldToken.IsRevoked);
        Assert.Equal(newToken.RefreshTokenId, oldToken.ReplacedByTokenId);
        Assert.DoesNotContain(await dbContext.RefreshTokens.ToArrayAsync(),
            token => token.TokenHash == oldRawToken || token.TokenHash == newRawToken);
    }

    private static CreateUserRequest NewUser(
        Guid schoolId,
        string username,
        string email,
        string password = "StrongPassword123!") =>
        new(schoolId, username, email, password, "Test", "User", null);

    private sealed record CreateUserRequest(
        Guid SchoolId, string Username, string Email, string Password,
        string FirstName, string LastName, string? PhoneNumber);
    private sealed record CreateUserResponse(Guid UserId);
    private sealed record CreateRoleRequest(Guid SchoolId, string Name, string? Description);
    private sealed record CreateRoleResponse(Guid RoleId);
    private sealed record LoginRequest(Guid SchoolId, string UsernameOrEmail, string Password);
    private sealed record LoginResponse(
        string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc,
        Guid UserId, string Username, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);
    private sealed record RefreshRequest(string RefreshToken);
    private sealed record RefreshResponse(
        string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
    private sealed record LogoutRequest(string RefreshToken);

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
