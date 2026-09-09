using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SmartSchool.Modules.Identity.IntegrationTests;

public sealed class SwaggerEndpointTests
{
    [Fact]
    public async Task DevelopmentSwagger_ExposesIdentityEndpoints()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var uiResponse = await client.GetAsync("/swagger/index.html");
        var documentResponse = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, uiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, documentResponse.StatusCode);

        using var document = JsonDocument.Parse(await documentResponse.Content.ReadAsStreamAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/identity/users", out _));
        Assert.True(paths.TryGetProperty("/api/identity/roles", out _));
        Assert.True(paths.TryGetProperty("/api/identity/users/{userId}/roles/{roleId}", out _));
        Assert.True(paths.TryGetProperty("/api/identity/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/identity/auth/refresh", out _));
        Assert.True(paths.TryGetProperty("/api/identity/auth/logout", out _));
    }
}
