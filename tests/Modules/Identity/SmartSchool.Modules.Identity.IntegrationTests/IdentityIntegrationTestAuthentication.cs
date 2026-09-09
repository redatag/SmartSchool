using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SmartSchool.Modules.Identity.IntegrationTests;

internal static class IdentityIntegrationTestAuthentication
{
    private const string Issuer = "SmartSchool.Tests";
    private const string Audience = "SmartSchool.Tests.Client";
    private const string Key = "test-only-signing-key-with-at-least-thirty-two-bytes";

    public static void ConfigureJwt(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:Key"] = Key
            }));

    public static HttpClient CreateClientWithPermission(
        this WebApplicationFactory<Program> factory,
        string permission)
    {
        var client = factory.CreateClient();
        client.UseBearerToken(CreateToken(permission));
        return client;
    }

    public static string CreateToken(string permission, DateTime? expiresAtUtc = null)
    {
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim("permission", permission)],
            notBefore: expiresAtUtc.HasValue && expiresAtUtc.Value <= now ? now.AddHours(-1) : now,
            expires: expiresAtUtc ?? now.AddMinutes(10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpClient UseBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
