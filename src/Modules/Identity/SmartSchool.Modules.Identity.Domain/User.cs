using System.Net.Mail;

namespace SmartSchool.Modules.Identity.Domain;

public sealed class User
{
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<UserRole> _userRoles = [];

    private User(
        Guid userId,
        Guid schoolId,
        string username,
        string normalizedUsername,
        string email,
        string normalizedEmail,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber,
        DateTime createdAtUtc)
    {
        UserId = userId;
        SchoolId = schoolId;
        Username = username;
        NormalizedUsername = normalizedUsername;
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
        Status = UserStatus.Active;
        CreatedAtUtc = createdAtUtc;
        _domainEvents.Add(new UserCreatedDomainEvent(userId, schoolId, createdAtUtc));
    }

    private User() { }

    public Guid UserId { get; private init; }
    public Guid SchoolId { get; private init; }
    public string Username { get; private init; } = string.Empty;
    public string NormalizedUsername { get; private init; } = string.Empty;
    public string Email { get; private init; } = string.Empty;
    public string NormalizedEmail { get; private init; } = string.Empty;
    public string PasswordHash { get; private init; } = string.Empty;
    public string FirstName { get; private init; } = string.Empty;
    public string LastName { get; private init; } = string.Empty;
    public string? PhoneNumber { get; private init; }
    public UserStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private init; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static User Create(
        Guid schoolId,
        string username,
        string normalizedUsername,
        string email,
        string normalizedEmail,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber = null)
    {
        if (schoolId == Guid.Empty) throw new IdentityDomainException("SchoolId is required.");

        username = Required(username, nameof(username), 100);
        normalizedUsername = Required(normalizedUsername, nameof(normalizedUsername), 100);
        email = Required(email, nameof(email), 256);
        normalizedEmail = Required(normalizedEmail, nameof(normalizedEmail), 256);
        passwordHash = Required(passwordHash, nameof(passwordHash), 500);
        firstName = Required(firstName, nameof(firstName), 100);
        lastName = Required(lastName, nameof(lastName), 100);
        phoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        if (!MailAddress.TryCreate(email, out _)) throw new IdentityDomainException("Email is invalid.");
        if (phoneNumber?.Length > 30) throw new IdentityDomainException("PhoneNumber cannot exceed 30 characters.");

        return new User(
            Guid.CreateVersion7(), schoolId, username, normalizedUsername, email, normalizedEmail,
            passwordHash, firstName, lastName, phoneNumber, DateTime.UtcNow);
    }

    public void AssignRole(Guid roleId)
    {
        if (roleId == Guid.Empty) throw new IdentityDomainException("RoleId is required.");
        if (_userRoles.Any(userRole => userRole.RoleId == roleId))
            throw new RoleAlreadyAssignedDomainException(UserId, roleId);

        var assignedAtUtc = DateTime.UtcNow;
        _userRoles.Add(UserRole.Create(UserId, roleId, assignedAtUtc));
        _domainEvents.Add(new RoleAssignedDomainEvent(UserId, roleId, assignedAtUtc));
    }

    public void Activate() => Status = UserStatus.Active;

    public void Deactivate() => Status = UserStatus.Inactive;

    public void Suspend() => Status = UserStatus.Suspended;

    public void RegisterLogin(DateTime utcNow)
    {
        if (Status != UserStatus.Active)
            throw new UserAuthenticationNotAllowedDomainException(UserId, Status);
        if (utcNow.Kind != DateTimeKind.Utc)
            throw new IdentityDomainException("Login timestamp must be UTC.");

        LastLoginAtUtc = utcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private static string Required(string value, string name, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new IdentityDomainException($"{name} is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength) throw new IdentityDomainException($"{name} cannot exceed {maximumLength} characters.");
        return trimmed;
    }
}

public enum UserStatus : byte
{
    Inactive = 0,
    Active = 1,
    Suspended = 2
}

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}

public sealed record UserCreatedDomainEvent(Guid UserId, Guid SchoolId, DateTime OccurredOnUtc) : IDomainEvent;

public sealed record RoleAssignedDomainEvent(Guid UserId, Guid RoleId, DateTime OccurredOnUtc) : IDomainEvent;

public class IdentityDomainException(string message) : Exception(message);

public sealed class RoleAlreadyAssignedDomainException(Guid userId, Guid roleId)
    : IdentityDomainException($"User '{userId}' already has role '{roleId}'.");

public sealed class UserAuthenticationNotAllowedDomainException(Guid userId, UserStatus status)
    : IdentityDomainException($"User '{userId}' cannot authenticate while in status '{status}'.");
