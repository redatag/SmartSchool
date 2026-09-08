namespace SmartSchool.Modules.Identity.Domain;

public sealed class Role
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private Role(
        Guid roleId,
        Guid schoolId,
        string name,
        string normalizedName,
        string? description,
        bool isSystemRole,
        DateTime createdAtUtc)
    {
        RoleId = roleId;
        SchoolId = schoolId;
        Name = name;
        NormalizedName = normalizedName;
        Description = description;
        IsSystemRole = isSystemRole;
        CreatedAtUtc = createdAtUtc;
        _domainEvents.Add(new RoleCreatedDomainEvent(roleId, schoolId, createdAtUtc));
    }

    private Role() { }

    public Guid RoleId { get; private init; }
    public Guid SchoolId { get; private init; }
    public string Name { get; private init; } = string.Empty;
    public string NormalizedName { get; private init; } = string.Empty;
    public string? Description { get; private init; }
    public bool IsSystemRole { get; private init; }
    public DateTime CreatedAtUtc { get; private init; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static Role Create(
        Guid schoolId,
        string name,
        string? description = null,
        bool isSystemRole = false)
    {
        if (schoolId == Guid.Empty) throw new IdentityDomainException("SchoolId is required.");

        name = Required(name, nameof(name), 100);
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description?.Length > 500)
            throw new IdentityDomainException("Description cannot exceed 500 characters.");

        return new Role(
            Guid.CreateVersion7(),
            schoolId,
            name,
            NormalizeName(name),
            description,
            isSystemRole,
            DateTime.UtcNow);
    }

    public static string NormalizeName(string name) => name.Trim().ToUpperInvariant();

    public void ClearDomainEvents() => _domainEvents.Clear();

    private static string Required(string value, string name, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new IdentityDomainException($"{name} is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > maximumLength)
            throw new IdentityDomainException($"{name} cannot exceed {maximumLength} characters.");
        return trimmed;
    }
}

public sealed record RoleCreatedDomainEvent(Guid RoleId, Guid SchoolId, DateTime OccurredOnUtc) : IDomainEvent;
