using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartSchool.Modules.Identity.Application;
using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Infrastructure.Security;

public sealed class JwtTokenProvider(IOptions<JwtOptions> options) : ITokenProvider
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokenResult GenerateAccessToken(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions)
    {
        ValidateOptions();

        var issuedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(_options.AccessTokenExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new("school_id", user.SchoolId.ToString()),
            new("username", user.Username)
        };
        claims.AddRange(roles.Distinct(StringComparer.Ordinal).Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Distinct(StringComparer.Ordinal).Select(permission => new Claim("permission", permission)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.Issuer))
            throw new InvalidOperationException("JWT issuer is required.");
        if (string.IsNullOrWhiteSpace(_options.Audience))
            throw new InvalidOperationException("JWT audience is required.");
        if (Encoding.UTF8.GetByteCount(_options.Key) < 32)
            throw new InvalidOperationException("JWT signing key must contain at least 32 bytes.");
        if (_options.AccessTokenExpirationMinutes <= 0)
            throw new InvalidOperationException("JWT access token expiration must be greater than zero.");
    }
}
