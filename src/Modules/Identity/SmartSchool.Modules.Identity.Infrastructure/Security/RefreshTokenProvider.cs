using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartSchool.Modules.Identity.Application;

namespace SmartSchool.Modules.Identity.Infrastructure.Security;

public sealed class RefreshTokenProvider(IOptions<JwtOptions> options) : IRefreshTokenProvider
{
    private readonly JwtOptions _options = options.Value;

    public GeneratedRefreshToken Generate()
    {
        if (_options.RefreshTokenExpirationDays <= 0)
            throw new InvalidOperationException("Refresh token expiration must be greater than zero.");

        var createdAtUtc = DateTime.UtcNow;
        var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return new GeneratedRefreshToken(
            token,
            Hash(token),
            createdAtUtc.AddDays(_options.RefreshTokenExpirationDays),
            createdAtUtc);
    }

    public string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
