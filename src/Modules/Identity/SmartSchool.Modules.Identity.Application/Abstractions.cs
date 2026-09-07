using SmartSchool.Modules.Identity.Domain;

namespace SmartSchool.Modules.Identity.Application;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken);
    void Add(User user);
}

public interface IRoleRepository
{
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken);
    Task<Role?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken);
    void Add(Role role);
}

public interface IIdentityUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

public interface ITokenService
{
    string CreateAccessToken(Guid userId, string email, IReadOnlyCollection<string> permissions);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
