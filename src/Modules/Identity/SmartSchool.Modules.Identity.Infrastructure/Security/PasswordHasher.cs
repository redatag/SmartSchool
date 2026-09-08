using Microsoft.AspNetCore.Identity;
using SmartSchool.Modules.Identity.Application;

namespace SmartSchool.Modules.Identity.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private static readonly object UserContext = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(UserContext, password);
}
